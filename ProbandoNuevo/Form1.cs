using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ProbandoNuevo
{
    public partial class Form1 : Form
    {
        // Obtener la instancia del servicio de reservas
        private readonly BookingService _bookingService;

        public Form1()
        {
            InitializeComponent();
            // Obtener la instancia única del servicio
            _bookingService = BookingService.Instance;
            
            // Suscribir el evento FormClosing para guardar los datos
            this.FormClosing += new FormClosingEventHandler(this.Form1_FormClosing);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Cargar las reservas para la fecha de hoy al iniciar
            RefreshBookingsGrid();
            lblStatus.Text = "Panel de control cargado.";
            UpdateToggleRestrictionButtonText(dtpViewDate.Value.Date); // Asegurarse de que el texto del botón sea correcto
        }
        
        private void RefreshBookingsGrid()
        {
            var selectedDate = dtpViewDate.Value.Date;
            var bookingsForDay = _bookingService.GetBookingsForDate(selectedDate);
            
            // Usamos una BindingList para que el grid se actualice mejor si hubiera cambios en tiempo real.
            dgvBookings.DataSource = new BindingList<Booking>(bookingsForDay.ToList());
            
            // Formatear las columnas del DataGridView para mejor legibilidad
            FormatDataGridView();
            UpdateUIBasedOnDateState(selectedDate);
        }

        private void UpdateUIBasedOnDateState(DateTime selectedDate)
        {
            if (_bookingService.IsDayRestricted(selectedDate))
            {
                dgvBookings.DefaultCellStyle.BackColor = Color.LightGray;
                dgvBookings.DefaultCellStyle.ForeColor = Color.DarkGray;
                btnAddNewBooking.Enabled = false;
                UpdateToggleRestrictionButtonText(selectedDate);
                lblStatus.Text = $"Día restringido. No se admiten nuevas reservas para el {selectedDate:dd/MM/yyyy}.";
            }
            else
            {
                dgvBookings.DefaultCellStyle.BackColor = SystemColors.Window;
                dgvBookings.DefaultCellStyle.ForeColor = SystemColors.ControlText;
                btnAddNewBooking.Enabled = true;
                UpdateToggleRestrictionButtonText(selectedDate);
                lblStatus.Text = $"Mostrando {dgvBookings.Rows.Count} reservas para el {selectedDate:dd/MM/yyyy}.";
            }
        }

        private void UpdateToggleRestrictionButtonText(DateTime date)
        {
             if (_bookingService.IsDayRestricted(date))
             {
                 btnToggleRestriction.Text = "Permitir Día";
             }
             else
             {
                 btnToggleRestriction.Text = "Restringir Día";
             }
        }

        private void FormatDataGridView()
        {
            if (dgvBookings.Columns.Count > 0)
            {
                dgvBookings.Columns["BookingId"].HeaderText = "ID Reserva";
                dgvBookings.Columns["CourtId"].Visible = false; // Ocultamos el ID de la pista
                dgvBookings.Columns["CourtName"].HeaderText = "Pista";
                dgvBookings.Columns["CustomerName"].HeaderText = "Cliente";
                dgvBookings.Columns["StartTime"].HeaderText = "Hora Inicio";
                dgvBookings.Columns["StartTime"].DefaultCellStyle.Format = "HH:mm";
                dgvBookings.Columns["EndTime"].HeaderText = "Hora Fin";
                dgvBookings.Columns["EndTime"].DefaultCellStyle.Format = "HH:mm";
                dgvBookings.Columns["TotalCost"].HeaderText = "Coste Total";
                dgvBookings.Columns["TotalCost"].DefaultCellStyle.Format = "C2"; // Formato de moneda
        
                // Formatear las nuevas columnas para una mejor visualización
                if (dgvBookings.Columns.Contains("PersonInCharge"))
                {
                    dgvBookings.Columns["PersonInCharge"].HeaderText = "Responsable";
                }
                if (dgvBookings.Columns.Contains("BringOwnBalls"))
                {
                    dgvBookings.Columns["BringOwnBalls"].HeaderText = "Pelotas Propias";
                }
            }
        }

        private void dtpViewDate_ValueChanged(object sender, EventArgs e)
        {
            // Refrescar la tabla cuando el usuario cambia la fecha
            RefreshBookingsGrid();
        }

        private void btnAddNewBooking_Click(object sender, EventArgs e)
        {
            var selectedDate = dtpViewDate.Value.Date;
            if (_bookingService.IsDayRestricted(selectedDate))
            {
                MessageBox.Show("No se pueden crear reservas en un día restringido.", "Día Restringido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Abrir un nuevo formulario para crear la reserva
            using (var newBookingForm = new NewBookingForm(selectedDate))
            {
                // Si el usuario cierra el formulario con "OK", refrescamos la tabla
                if (newBookingForm.ShowDialog() == DialogResult.OK)
                {
                    RefreshBookingsGrid();
                    lblStatus.Text = "¡Nueva reserva creada con éxito!";
                }
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)
        {
            RefreshBookingsGrid();
        }

        private void btnToggleRestriction_Click(object sender, EventArgs e)
        {
            var selectedDate = dtpViewDate.Value.Date;
            _bookingService.ToggleDayRestriction(selectedDate);
            RefreshBookingsGrid(); // Actualizar la UI para reflejar el cambio
        }

        private void salirToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void editarReservaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dgvBookings.SelectedRows.Count == 0) return;

            var selectedRow = dgvBookings.SelectedRows[0];
            var bookingId = (int)selectedRow.Cells["BookingId"].Value;
            var bookingToEdit = _bookingService.GetBookingById(bookingId);

            if (bookingToEdit != null)
            {
                using (var editForm = new NewBookingForm(bookingToEdit))
                {
                    if (editForm.ShowDialog() == DialogResult.OK)
                    {
                        RefreshBookingsGrid();
                        lblStatus.Text = $"Reserva {bookingId} actualizada con éxito.";
                    }
                }
            }
        }

        private void eliminarReservaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (dgvBookings.SelectedRows.Count == 0) return;

            var selectedRow = dgvBookings.SelectedRows[0];
            var bookingId = (int)selectedRow.Cells["BookingId"].Value;

            var confirmResult = MessageBox.Show($"¿Está seguro de que desea eliminar la reserva {bookingId}?",
                                                 "Confirmar Eliminación",
                                                 MessageBoxButtons.YesNo,
                                                 MessageBoxIcon.Warning);
            if (confirmResult == DialogResult.Yes)
            {
                if (_bookingService.DeleteBooking(bookingId))
                {
                    RefreshBookingsGrid();
                    lblStatus.Text = $"Reserva {bookingId} eliminada.";
                }
            }
        }

        private void dgvBookings_MouseClick(object sender, MouseEventArgs e)
        {
            // Seleccionar la fila con el clic derecho para que el menú contextual sepa sobre qué fila actuar
            if (e.Button == MouseButtons.Right)
            {
                var hitTestInfo = dgvBookings.HitTest(e.X, e.Y);
                if (hitTestInfo.RowIndex >= 0)
                {
                    dgvBookings.ClearSelection();
                    dgvBookings.Rows[hitTestInfo.RowIndex].Selected = true;
                }
            }
        }

        // Nuevo método para manejar el cierre del formulario y guardar los datos
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                _bookingService.SaveData();
            }
            catch (Exception ex)
            {
                var result = MessageBox.Show($"Error al guardar los datos: {ex.Message}\n\n¿Desea cerrar la aplicación de todas formas? Se podrían perder los cambios.", "Error al Guardar", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
                if (result == DialogResult.No)
                {
                    e.Cancel = true; // Si hay un error al guardar y el usuario no quiere salir, cancelar el cierre.
                }
            }
        }
    }
}