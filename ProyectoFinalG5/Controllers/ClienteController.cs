using Microsoft.AspNetCore.Mvc;
using ProyectoFinalG5.Data;
using ProyectoFinalG5.Models;

namespace ProyectoFinalG5.Controllers
{
    public class ClienteController : Controller
    {
        private readonly AppDbContext _context;

        public ClienteController(AppDbContext context)
        {
            _context = context;
        }

        // --- MÉTODO FALTANTE QUE SOLUCIONA EL ERROR 404 ---
        public IActionResult Listar()
        {
            var clientes = _context.Clientes.ToList();
            return View(clientes);
        }

        public IActionResult Nuevo()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Nuevo(Cliente c)
        {
            // Limpia cualquier error previo o automático en inglés
            ModelState.Clear();

            // === VALIDACIÓN PARA EL CP-02: CAMPOS OBLIGATORIOS VACÍOS ===
            if (string.IsNullOrWhiteSpace(c.Nombre))
            {
                ModelState.AddModelError("Nombre", "El nombre es obligatorio.");
            }

            if (string.IsNullOrWhiteSpace(c.Telefono))
            {
                ModelState.AddModelError("Telefono", "El teléfono es obligatorio.");
            }
            // ============================================================

            // Si hay algún campo vacío, el ModelState no será válido y se detiene el proceso
            if (!ModelState.IsValid)
            {
                // Retorna la misma vista mostrando los mensajes de error configurados arriba
                return View(c);
            }

            var email = HttpContext.Session.GetString("EmailUsuario");

            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login", "Usuario");

            c.Correo = email;

            _context.Clientes.Add(c);
            _context.SaveChanges();

            return RedirectToAction("Listar", "Reserva");
        }

        [HttpPost]
        public IActionResult Crear(Cliente c)
        {
            if (!ModelState.IsValid)
            {
                return View("Crear", c);
            }

            var email = HttpContext.Session.GetString("EmailUsuario");

            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Login", "Usuario");

            c.Correo = email;

            _context.Clientes.Add(c);
            _context.SaveChanges();

            return RedirectToAction("Crear", "Pago");
        }

        [HttpPost]
        public IActionResult Editar(Cliente c)
        {
            var rol = HttpContext.Session.GetString("Rol");

            if (rol != "1" && rol != "2")
                return RedirectToAction("Listar", "Reserva");

            var cliente = _context.Clientes.FirstOrDefault(x => x.IdCliente == c.IdCliente);

            if (cliente == null)
                return RedirectToAction("Listar", "Reserva");

            cliente.Nombre = c.Nombre;
            cliente.Telefono = c.Telefono;

            _context.SaveChanges();

            return RedirectToAction("Listar");
        }

        public IActionResult Eliminar(int id)
        {
            // PRUEBA 1
            if (HttpContext == null)
                throw new Exception("ERROR: HttpContext es null");

            // PRUEBA 2
            if (HttpContext.Session == null)
                throw new Exception("ERROR: Session es null");

            // Obtener rol
            var rol = HttpContext.Session.GetString("Rol");

            if (rol != "1")
                return RedirectToAction("Listar", "Reserva");

            // PRUEBA 3
            if (_context == null)
                throw new Exception("ERROR: _context es null");

            // Buscar cliente
            var cliente = _context.Clientes.Find(id);

            if (cliente == null)
                return RedirectToAction("Listar");

            // PRUEBA 4
            if (_context.Reservas == null)
                throw new Exception("ERROR: _context.Reservas es null");

            // Verificar si el cliente tiene cualquier reserva asignada
            var tieneReservas = _context.Reservas.Any(r => r.IdCliente == id);

            if (tieneReservas)
            {
                // PRUEBA 5
                if (TempData == null)
                    throw new Exception("ERROR: TempData es null");

                TempData["Error"] =
                    "No se puede eliminar el cliente porque tiene reservas asignadas. Primero debes borrar sus reservas.";

                return RedirectToAction("Listar");
            }

            _context.Clientes.Remove(cliente);
            _context.SaveChanges();

            return RedirectToAction("Listar");
        }
    }
}