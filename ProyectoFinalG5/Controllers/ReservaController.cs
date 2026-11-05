using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoFinalG5.Data;
using ProyectoFinalG5.Models;

namespace ProyectoFinalG5.Controllers
{
    public class ReservaController : Controller
    {
        private readonly AppDbContext _context;

        public ReservaController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Listar()
        {
            ActualizarEstados();

            var reservas = _context.Reservas
                .Include(r => r.Cliente)
                .Include(r => r.Empleado)
                .Include(r => r.HorarioFijo)
                .Include(r => r.SalaEstudio)
                .Include(r => r.Pago)
                .Include(r => r.ReservaServicios)
                    .ThenInclude(rs => rs.Servicio)
                        .ThenInclude(s => s.TipoServicio)
                .ToList();

            return View(reservas);
        }

        public IActionResult Nuevo(int? idServicio)
        {
            CargarCombos();

            ViewBag.MetodosPago = new List<string>
    {
        "Efectivo",
        "Yape",
        "Transferencia",
        "Tarjeta"
    };


            ViewBag.ServicioSeleccionado = idServicio;


            return View();
        }


        [HttpPost]
        public IActionResult Nuevo(Reserva r, int[] serviciosSeleccionados, string metodoPago)
        {
            CargarCombos();

            ViewBag.MetodosPago = new List<string>
            {
                "Efectivo",
                "Yape",
                "Transferencia",
                "Tarjeta"
            };

            ViewBag.ServicioSeleccionado = serviciosSeleccionados?.FirstOrDefault();

            // =========================================================================
            // VALIDACIÓN DE CAMPOS OBLIGATORIOS (Sin alterar el funcionamiento original)
            // =========================================================================
            if (r.IdCliente <= 0)
            {
                ModelState.AddModelError("IdCliente", "El cliente es obligatorio.");
            }

            if (r.IdEmpleado <= 0)
            {
                ModelState.AddModelError("IdEmpleado", "El empleado es obligatorio.");
            }

            if (r.IdHorarioFijo <= 0)
            {
                ModelState.AddModelError("IdHorarioFijo", "El horario es obligatorio.");
            }

            if (r.IdSala <= 0)
            {
                ModelState.AddModelError("IdSala", "La sala es obligatoria.");
            }

            if (string.IsNullOrEmpty(metodoPago))
            {
                ModelState.AddModelError("", "El método de pago es obligatorio.");
            }

            if (r.FechaReserva == default)
            {
                ModelState.AddModelError("FechaReserva", "La fecha de reserva es obligatoria.");
            }

            // =========================================================================
            // VALIDACIÓN DE FECHA Y HORA PASADA
            // =========================================================================
            if (r.FechaReserva != default && r.IdHorarioFijo > 0)
            {
                var horario = _context.HorariosFijos.Find(r.IdHorarioFijo);
                if (horario != null)
                {
                    var fechaHoraReserva = r.FechaReserva.Date + horario.HoraInicio;

                    if (fechaHoraReserva <= DateTime.Now)
                    {
                        ModelState.AddModelError("", "No se puede registrar una reserva en una fecha u hora que ya ha pasado.");
                    }
                }
            }
            // =========================================================================

            if (!ModelState.IsValid)
                return View(r);

            var ocupado = _context.Reservas.Any(x =>
                x.IdHorarioFijo == r.IdHorarioFijo &&
                x.FechaReserva == r.FechaReserva);

            if (ocupado)
            {
                ModelState.AddModelError("", "El horario seleccionado ya está ocupado");
                return View(r);
            }

            decimal total = 0;

            if (serviciosSeleccionados != null)
            {
                foreach (var id in serviciosSeleccionados)
                {
                    var servicio = _context.Servicios.Find(id);

                    if (servicio != null)
                    {
                        total += servicio.Precio;
                    }
                }
            }

            var pago = new Pago
            {
                Monto = total,
                MetodoPago = metodoPago
            };

            _context.Pagos.Add(pago);
            _context.SaveChanges();

            var reserva = new Reserva
            {
                FechaReserva = r.FechaReserva,
                IdCliente = r.IdCliente,
                IdEmpleado = r.IdEmpleado,
                IdHorarioFijo = r.IdHorarioFijo,
                IdSala = r.IdSala,
                IdPago = pago.IdPago,
                Estado = "Pendiente"
            };

            _context.Reservas.Add(reserva);
            _context.SaveChanges();

            List<ReservaServicio> detalles = new();

            if (serviciosSeleccionados != null)
            {
                foreach (var id in serviciosSeleccionados)
                {
                    var servicio = _context.Servicios.Find(id);

                    if (servicio != null)
                    {
                        detalles.Add(new ReservaServicio
                        {
                            IdReserva = reserva.IdReserva,
                            IdServicio = servicio.IdServicio,
                            Precio = servicio.Precio
                        });
                    }
                }
            }

            _context.ReservaServicios.AddRange(detalles);
            _context.SaveChanges();

            return RedirectToAction("Listar");
        }

        public IActionResult Editar(int id)
        {
            var reserva = _context.Reservas
                .Include(r => r.ReservaServicios)
                    .ThenInclude(rs => rs.Servicio)
                .Include(r => r.Pago)
                .FirstOrDefault(r => r.IdReserva == id);

            if (reserva == null)
                return RedirectToAction("Listar");

            CargarCombos();

            ViewBag.MetodosPago = new List<string>
            {
                "Efectivo",
                "Yape",
                "Transferencia"
            };

            return View(reserva);
        }

        [HttpPost]
        public IActionResult Editar(Reserva r, int[] serviciosSeleccionados, string metodoPago)
        {
            var reserva = _context.Reservas
                .Include(x => x.ReservaServicios)
                .FirstOrDefault(x => x.IdReserva == r.IdReserva);

            if (reserva == null)
                return RedirectToAction("Listar");

            // =========================================================================
            // NUEVA VALIDACIÓN EN EDITAR: Impedir cambiar a fecha/hora pasada
            // =========================================================================
            if (r.FechaReserva != default && r.IdHorarioFijo > 0)
            {
                var horario = _context.HorariosFijos.Find(r.IdHorarioFijo);
                if (horario != null)
                {
                    var fechaHoraReserva = r.FechaReserva.Date + horario.HoraInicio;

                    if (fechaHoraReserva <= DateTime.Now)
                    {
                        CargarCombos();
                        ViewBag.MetodosPago = new List<string> { "Efectivo", "Yape", "Transferencia", "Tarjeta" };
                        ModelState.AddModelError("", "No se puede actualizar una reserva a una fecha u hora que ya ha pasado.");
                        return View(reserva);
                    }
                }
            }
            // =========================================================================

            reserva.IdCliente = r.IdCliente;
            reserva.IdEmpleado = r.IdEmpleado;
            reserva.IdHorarioFijo = r.IdHorarioFijo;
            reserva.IdSala = r.IdSala;
            reserva.FechaReserva = r.FechaReserva;
            reserva.Estado = r.Estado;

            var antiguos = _context.ReservaServicios
                .Where(x => x.IdReserva == reserva.IdReserva)
                .ToList();

            _context.ReservaServicios.RemoveRange(antiguos);

            decimal total = 0;
            List<ReservaServicio> nuevos = new();

            if (serviciosSeleccionados != null)
            {
                foreach (var id in serviciosSeleccionados)
                {
                    var servicio = _context.Servicios.Find(id);

                    if (servicio != null)
                    {
                        total += servicio.Precio;

                        nuevos.Add(new ReservaServicio
                        {
                            IdReserva = reserva.IdReserva,
                            IdServicio = servicio.IdServicio,
                            Precio = servicio.Precio
                        });
                    }
                }
            }

            _context.ReservaServicios.AddRange(nuevos);

            var pago = _context.Pagos.FirstOrDefault(x => x.IdPago == reserva.IdPago);

            if (pago != null)
            {
                pago.Monto = total;
                pago.MetodoPago = metodoPago;
            }

            _context.SaveChanges();

            return RedirectToAction("Listar");
        }

        public IActionResult Eliminar(int id)
        {

            var rol = HttpContext.Session.GetString("Rol");


            if (rol == "3")
                return RedirectToAction("Listar", "Reserva");

            var reserva = _context.Reservas
                .Include(r => r.ReservaServicios)
                .FirstOrDefault(r => r.IdReserva == id);

            if (reserva == null)
                return RedirectToAction("Listar");


            var servicios = _context.ReservaServicios
                .Where(x => x.IdReserva == id)
                .ToList();

            _context.ReservaServicios.RemoveRange(servicios);


            _context.Reservas.Remove(reserva);

            _context.SaveChanges();

            return RedirectToAction("Listar");
        }

        private void CargarCombos()
        {
            ViewBag.Clientes = _context.Clientes.ToList();
            ViewBag.Empleados = _context.Empleados.ToList();
            ViewBag.Servicios = _context.Servicios.ToList();
            ViewBag.Salas = _context.SalaEstudios.ToList();

            ViewBag.Horarios = _context.HorariosFijos
                .OrderBy(h => h.HoraInicio)
                .ToList();
        }

        private void ActualizarEstados()
        {
            var reservas = _context.Reservas
                .Include(r => r.HorarioFijo)
                .Where(r => r.Estado == "Pendiente")
                .ToList();

            foreach (var r in reservas)
            {
                var fin = r.FechaReserva.Date + r.HorarioFijo.HoraFin;

                if (fin <= DateTime.Now)
                {
                    r.Estado = "Realizado";
                }
            }

            _context.SaveChanges();
        }
    }
}