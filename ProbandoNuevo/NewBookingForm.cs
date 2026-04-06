using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ProbandoNuevo
{
    public partial class NewBookingForm : Form
    {
        private readonly BookingService _bookingService;
        private readonly Booking _editingBooking; // Null si es una nueva reserva

        // Constructor para NUEVA reserva
        public NewBookingForm(DateTime selectedDate)
        {
            InitializeComponent();
            _bookingService = BookingService.Instance;
            _editingBooking = null; // Indica que es una nueva reserva
            dtpBookingDate.Value = selectedDate;
        }

        // Constructor para EDITAR reserva
        public NewBookingForm(Booking bookingToEdit)
        {
            InitializeComponent();
            _bookingService = BookingService.Instance;
            _editingBooking = bookingToEdit; // La reserva a editar
        }

        private void NewBookingForm_Load(object sender, EventArgs e)
        {
            // Cargar datos comunes
            LoadCourts();
            LoadPromotions();
            LoadTimeSlots();

            // Asociar evento para actualizar la hora de fin cuando cambia la hora de inicio
            cmbStartTime.SelectedIndexChanged += (s, ev) => UpdateEndTimeOptions();

            // Asociar evento para recalcular precio si cambia la opción de pelotas
            chkBringOwnBalls.CheckedChanged += (s, ev) => btnCheckPrice_Click(s, ev);

            if (_editingBooking != null)
            {
                // Modo Edición: Cargar datos de la reserva existente
                this.Text = "Editar Reserva";
                btnConfirmBooking.Text = "Guardar Cambios";
                PopulateFormForEdit();
            }
            else
            {
                // Modo Creación: Comportamiento por defecto
                this.Text = "Nueva Reserva";
                cmbEndTime.Enabled = false;
                txtPersonInCharge.Text = Environment.UserName; // Sugerir el nombre de usuario actual
            }
        }

        private void PopulateFormForEdit()
        {
            txtCustomerName.Text = _editingBooking.CustomerName;
            dtpBookingDate.Value = _editingBooking.StartTime.Date;
            cmbCourt.SelectedValue = _editingBooking.CourtId;
            txtPersonInCharge.Text = _editingBooking.PersonInCharge; // Cargar encargado
            chkBringOwnBalls.Checked = _editingBooking.BringOwnBalls; // Cargar opción de pelotas

            // Seleccionar horas (asegurándose que existan en el ComboBox)
            string startTimeStr = _editingBooking.StartTime.ToString("HH:mm");
            string endTimeStr = _editingBooking.EndTime.ToString("HH:mm");

            if (cmbStartTime.Items.Contains(startTimeStr))
            {
                cmbStartTime.SelectedItem = startTimeStr;
            }
            UpdateEndTimeOptions(); // Cargar las opciones de hora de fin
            if (cmbEndTime.Items.Contains(endTimeStr))
            {
                cmbEndTime.SelectedItem = endTimeStr;
            }

            // Forzar una comprobación inicial para mostrar el precio y estado
            btnCheckPrice_Click(this, EventArgs.Empty);
        }

        private void LoadCourts()
        {
            cmbCourt.DataSource = _bookingService.Courts;
            cmbCourt.DisplayMember = "Name";
            cmbCourt.ValueMember = "Id";
        }

        private void LoadPromotions()
        {
            var promotions = _bookingService.Promotions.Select(p => p.Code).ToList();
            promotions.Insert(0, "NINGUNA");
            cmbPromotion.DataSource = promotions;
        }

        private void LoadTimeSlots()
        {
            for (int i = 9; i <= 22; i++)
            {
                cmbStartTime.Items.Add($"{i:00}:00");
            }
            if (cmbStartTime.Items.Count > 0 && _editingBooking == null)
                cmbStartTime.SelectedIndex = 0;
        }

        private void UpdateEndTimeOptions()
        {
            cmbEndTime.Items.Clear();
            if (cmbStartTime.SelectedItem == null) return;

            var startTime = TimeSpan.Parse(cmbStartTime.SelectedItem.ToString());

            for (int i = 1; i <= 2; i++)
            {
                var endTime = startTime.Add(TimeSpan.FromHours(i));
                if (endTime.TotalHours > 23) break;
                cmbEndTime.Items.Add(endTime.ToString(@"hh\:mm"));

                // Si la reserva en edición termina a esta hora, la preseleccionamos
                if (_editingBooking != null && _editingBooking.EndTime.TimeOfDay == endTime)
                {
                    cmbEndTime.SelectedItem = endTime.ToString(@"hh\:mm");
                }
            }

            cmbEndTime.Enabled = cmbEndTime.Items.Count > 0;
            // Si el modo es creación y no hay nada seleccionado, seleccionar el primer elemento
            if (_editingBooking == null && cmbEndTime.Items.Count > 0 && cmbEndTime.SelectedItem == null)
                cmbEndTime.SelectedIndex = 0;
        }

        private void btnCheckPrice_Click(object sender, EventArgs e)
        {
            if (cmbCourt.SelectedItem == null || cmbStartTime.SelectedItem == null || cmbEndTime.SelectedItem == null)
            {
                // Evitamos el mensaje de error cuando se carga el formulario en modo edición.
                if (_editingBooking == null || this.Visible)
                {
                    MessageBox.Show("Por favor, seleccione una pista, hora de inicio y hora de fin.", "Datos incompletos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return;
            }
            
            var courtId = (int)cmbCourt.SelectedValue;
            var date = dtpBookingDate.Value.Date;
            var startTime = date.Add(TimeSpan.Parse(cmbStartTime.SelectedItem.ToString()));
            var endTime = date.Add(TimeSpan.Parse(cmbEndTime.SelectedItem.ToString()));
            var bringOwnBalls = chkBringOwnBalls.Checked; // Nuevo: obtener el valor de si trae pelotas

            // Al editar, excluir la reserva actual de la comprobación de disponibilidad
            var bookingIdToExclude = _editingBooking?.BookingId ?? 0;

            if (_bookingService.IsCourtAvailable(courtId, startTime, endTime, bookingIdToExclude))
            {
                lblAvailability.Text = "Horario Disponible";
                lblAvailability.ForeColor = Color.Green;
                btnConfirmBooking.Enabled = true;

                string promoCode = cmbPromotion.SelectedItem.ToString() == "NINGUNA" ? null : cmbPromotion.SelectedItem.ToString();
                // Nuevo: pasar bringOwnBalls a CalculateCost
                var cost = _bookingService.CalculateCost(courtId, startTime, endTime, promoCode, bringOwnBalls);
                txtTotalCost.Text = cost.ToString("C2");
            }
            else
            {
                lblAvailability.Text = "Horario Ocupado";
                lblAvailability.ForeColor = Color.Red;
                btnConfirmBooking.Enabled = false;
                txtTotalCost.Text = "";
            }
        }

        private void btnConfirmBooking_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtCustomerName.Text))
            {
                MessageBox.Show("Por favor, introduzca el nombre del cliente.", "Datos incompletos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(txtPersonInCharge.Text))
            {
                MessageBox.Show("Por favor, introduzca el nombre del responsable.", "Datos incompletos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var courtId = (int)cmbCourt.SelectedValue;
            var date = dtpBookingDate.Value.Date;
            var startTime = date.Add(TimeSpan.Parse(cmbStartTime.SelectedItem.ToString()));
            var endTime = date.Add(TimeSpan.Parse(cmbEndTime.SelectedItem.ToString()));
            string promoCode = cmbPromotion.SelectedItem.ToString() == "NINGUNA" ? null : cmbPromotion.SelectedItem.ToString();
            var bringOwnBalls = chkBringOwnBalls.Checked; // Nuevo: obtener el valor de si trae pelotas

            if (_editingBooking == null) // Modo Creación
            {
                var newBooking = new Booking
                {
                    CourtId = courtId,
                    CustomerName = txtCustomerName.Text,
                    StartTime = startTime,
                    EndTime = endTime,
                    PersonInCharge = txtPersonInCharge.Text, // Nuevo: asignar encargado
                    BringOwnBalls = bringOwnBalls // Nuevo: asignar opción de pelotas
                };
                var createdBooking = _bookingService.MakeBooking(newBooking, promoCode);
                
                if (createdBooking != null)
                {
                     this.DialogResult = DialogResult.OK;
                     this.Close();
                }
                else
                {
                    MessageBox.Show("No se pudo realizar la reserva. El horario podría haberse ocupado.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else // Modo Edición
            {
                _editingBooking.CourtId = courtId;
                _editingBooking.CustomerName = txtCustomerName.Text;
                _editingBooking.StartTime = startTime;
                _editingBooking.EndTime = endTime;
                _editingBooking.PersonInCharge = txtPersonInCharge.Text; // Nuevo: actualizar encargado
                _editingBooking.BringOwnBalls = bringOwnBalls; // Nuevo: actualizar opción de pelotas

                if (_bookingService.UpdateBooking(_editingBooking, promoCode))
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                     MessageBox.Show("No se pudieron guardar los cambios. El nuevo horario podría no estar disponible.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}