using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace ProbandoNuevo
{
    #region Modelos de Datos

    public enum SportType
    {
        Tenis,
        Padel,
        Futbol
    }

    public class Court
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public SportType Sport { get; set; }
        public decimal HourlyRate { get; set; } // Precio por hora
    }

    public class Booking
    {
        public int BookingId { get; set; }
        public int CourtId { get; set; }
        public string CustomerName { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public decimal TotalCost { get; set; }
        public string PersonInCharge { get; set; }
        public bool BringOwnBalls { get; set; }

        [JsonIgnore] // Para evitar guardarla en el JSON y asignarla dinámicamente
        public string CourtName { get; set; }
    }

    public class Promotion
    {
        public string Code { get; set; }
        public string Description { get; set; }
        public decimal DiscountPercentage { get; set; } // Ej: 0.10M para 10%
    }

    // Clase auxiliar para la serialización
    public class AppData
    {
        public BindingList<Booking> Bookings { get; set; }
        public List<DateTime> RestrictedDays { get; set; }
    }

    #endregion

    #region Servicio de Lógica de Negocio

    public class BookingService
    {
        private static readonly BookingService _instance = new BookingService();
        public static BookingService Instance => _instance;

        public BindingList<Booking> Bookings { get; private set; }
        public List<Court> Courts { get; private set; }
        public List<Promotion> Promotions { get; private set; }
        private List<DateTime> _restrictedDays;

        private const string DataFileName = "bookingData.json";

        private BookingService()
        {
            InitializeDefaultData(); // Inicializar datos estáticos
            LoadData(); // Cargar datos dinámicos (reservas, días restringidos)
        }

        private void InitializeDefaultData()
        {
            Courts = new List<Court>
            {
                new Court { Id = 1, Name = "Pista de Tenis Central", Sport = SportType.Tenis, HourlyRate = 15.50M },
                new Court { Id = 2, Name = "Pista de Pádel 1", Sport = SportType.Padel, HourlyRate = 12.00M },
                new Court { Id = 3, Name = "Campo de Fútbol Sala", Sport = SportType.Futbol, HourlyRate = 25.00M }
            };

            Promotions = new List<Promotion>
            {
                new Promotion { Code = "DESC10", Description = "10% de descuento general", DiscountPercentage = 0.10M },
                new Promotion { Code = "MAÑANAS", Description = "20% antes de las 12:00", DiscountPercentage = 0.20M }
            };

            Bookings = new BindingList<Booking>();
            _restrictedDays = new List<DateTime>();
        }

        public IEnumerable<Booking> GetBookingsForDate(DateTime date)
        {
            var bookingsOnDate = Bookings.Where(b => b.StartTime.Date == date.Date).ToList();
            // Asignar el nombre de la pista para mostrar en el DataGridView
            foreach (var booking in bookingsOnDate)
            {
                booking.CourtName = Courts.FirstOrDefault(c => c.Id == booking.CourtId)?.Name;
            }
            return bookingsOnDate.OrderBy(b => b.StartTime).ThenBy(b => b.CourtId);
        }

        public Booking GetBookingById(int bookingId)
        {
            return Bookings.FirstOrDefault(b => b.BookingId == bookingId);
        }

        public bool IsCourtAvailable(int courtId, DateTime start, DateTime end, int bookingIdToExclude = 0)
        {
            return !Bookings.Any(b =>
                b.BookingId != bookingIdToExclude &&
                b.CourtId == courtId &&
                start < b.EndTime &&
                end > b.StartTime);
        }

        public decimal CalculateCost(int courtId, DateTime start, DateTime end, string promoCode, bool bringOwnBalls)
        {
            var court = Courts.FirstOrDefault(c => c.Id == courtId);
            if (court == null) throw new ArgumentException("Pista no encontrada.");

            var duration = (end - start).TotalHours;
            var baseCost = (decimal)duration * court.HourlyRate;
            var finalCost = baseCost;

            if (!string.IsNullOrEmpty(promoCode))
            {
                var promotion = Promotions.FirstOrDefault(p => p.Code.Equals(promoCode, StringComparison.OrdinalIgnoreCase));
                if (promotion != null)
                {
                    finalCost -= baseCost * promotion.DiscountPercentage;
                }
            }
        
            if (!bringOwnBalls)
            {
                finalCost += 2.50M;
            }

            return finalCost < 0 ? 0 : finalCost;
        }

        public Booking MakeBooking(Booking newBooking, string promoCode)
        {
            if (!IsCourtAvailable(newBooking.CourtId, newBooking.StartTime, newBooking.EndTime))
                return null;

            newBooking.BookingId = Bookings.Any() ? Bookings.Max(b => b.BookingId) + 1 : 1;
            newBooking.TotalCost = CalculateCost(newBooking.CourtId, newBooking.StartTime, newBooking.EndTime, promoCode, newBooking.BringOwnBalls);
            
            Bookings.Add(newBooking);
            return newBooking;
        }

        public bool UpdateBooking(Booking bookingToUpdate, string promoCode)
        {
            var existingBooking = Bookings.FirstOrDefault(b => b.BookingId == bookingToUpdate.BookingId);
            if (existingBooking == null) return false;

            if (!IsCourtAvailable(bookingToUpdate.CourtId, bookingToUpdate.StartTime, bookingToUpdate.EndTime, bookingToUpdate.BookingId))
                return false;

            existingBooking.CourtId = bookingToUpdate.CourtId;
            existingBooking.CustomerName = bookingToUpdate.CustomerName;
            existingBooking.StartTime = bookingToUpdate.StartTime;
            existingBooking.EndTime = bookingToUpdate.EndTime;
            existingBooking.PersonInCharge = bookingToUpdate.PersonInCharge;
            existingBooking.BringOwnBalls = bookingToUpdate.BringOwnBalls;
            existingBooking.TotalCost = CalculateCost(existingBooking.CourtId, existingBooking.StartTime, existingBooking.EndTime, promoCode, existingBooking.BringOwnBalls);
            
            Bookings.ResetBindings(); 
            return true;
        }

        public bool DeleteBooking(int bookingId)
        {
            var bookingToRemove = Bookings.FirstOrDefault(b => b.BookingId == bookingId);
            if (bookingToRemove != null)
            {
                Bookings.Remove(bookingToRemove);
                return true;
            }
            return false;
        }

        public bool IsDayRestricted(DateTime date) => _restrictedDays.Contains(date.Date);

        public void ToggleDayRestriction(DateTime date)
        {
            if (IsDayRestricted(date.Date))
                _restrictedDays.Remove(date.Date);
            else
                _restrictedDays.Add(date.Date);
        }

        public void SaveData()
        {
            var dataToSave = new AppData
            {
                Bookings = this.Bookings,
                RestrictedDays = this._restrictedDays
            };
            var json = JsonConvert.SerializeObject(dataToSave, Formatting.Indented);
            File.WriteAllText(DataFileName, json);
        }

        public void LoadData()
        {
            if (File.Exists(DataFileName))
            {
                var json = File.ReadAllText(DataFileName);
                var loadedData = JsonConvert.DeserializeObject<AppData>(json);
                if (loadedData != null)
                {
                    Bookings = new BindingList<Booking>(loadedData.Bookings.ToList());
                    _restrictedDays = loadedData.RestrictedDays ?? new List<DateTime>();
                }
            }
        }
    }

    #endregion
}