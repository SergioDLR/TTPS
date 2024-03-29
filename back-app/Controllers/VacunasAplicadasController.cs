﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VacunacionApi.DTO;
using VacunacionApi.Models;
using VacunacionApi.ModelsDataWareHouse;
using VacunacionApi.Services;

namespace VacunacionApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VacunasAplicadasController : ControllerBase
    {
        private readonly VacunasContext _context;
        private readonly DataWareHouseContext _context2;

        public VacunasAplicadasController(VacunasContext context, DataWareHouseContext context2)
        {
            _context = context;
            _context2 = context2;
        }

        // GET: api/VacunasAplicadas/GetAll?emailUsuario=juan@gmail.com&skip=20&take=40&descripcionJurisdiccion=Buenos Aires
        [HttpGet]
        [Route("GetAll")]
        public async Task<ActionResult<IEnumerable<ResponseListaVacunasAplicadasDTO>>> GetAll(string emailUsuario = null, int skip = 0, int take = 0, string descripcionJurisdiccion = null)
        {
            try
            {
                ResponseListaVacunasAplicadasDTO responseListaVacunasAplicadas = new ResponseListaVacunasAplicadasDTO();

                List<string> errores = new List<string>();
                List<VacunaAplicadaConsultaDTO> vacunasAplicadasDTO = new List<VacunaAplicadaConsultaDTO>();
                bool existenciaErrores = true;
                string transaccion = "Rechazada";
                Rol rolUsuario = null;

                Usuario usuarioExistente = await CuentaUsuarioExists(emailUsuario);
                if (usuarioExistente == null)
                {
                    errores.Add(String.Format("El usuario {0} no existe en el sistema", emailUsuario));
                }
                else
                {
                    rolUsuario = await GetRol(usuarioExistente.IdRol);
                }

                List<VacunaAplicada> vacunasAplicadas = await _context.VacunaAplicada.ToListAsync();
                if (skip != 0 && take != 0)
                    vacunasAplicadas = vacunasAplicadas.Skip(skip).Take(take).ToList();

                if (descripcionJurisdiccion != null)
                {
                    Jurisdiccion juris = await GetJurisdiccionByDescripcion(descripcionJurisdiccion);
                    vacunasAplicadas = vacunasAplicadas.Where(v => v.IdJurisdiccion == juris.Id).ToList();
                }
                    
                if (vacunasAplicadas.Count == 0)
                {
                    errores.Add("No existen vacunados en la base de datos");
                }

                if (rolUsuario != null)
                {
                    if (rolUsuario.Descripcion == "Administrador" || rolUsuario.Descripcion == "Vacunador")
                    {
                        errores.Add(String.Format("El rol {0} no tiene permisos para visualizar esta sección", rolUsuario.Descripcion));
                    }
                }

                if (errores.Count == 0)
                {
                    transaccion = "Aceptada";
                    existenciaErrores = false;

                    if (rolUsuario.Descripcion == "Analista Provincial")
                    {
                        foreach (VacunaAplicada item in vacunasAplicadas)
                        {
                            Jurisdiccion jurisdiccion = await GetDescripcionJurisdiccion(item.IdJurisdiccion);
                            Lote lote = await GetLote(item.IdLote);
                            VacunaDesarrollada vacunaDesarrollada = await GetVacunaDesarrollada(lote.IdVacunaDesarrollada);
                            Vacuna vacuna = await GetVacuna(vacunaDesarrollada.IdVacuna);
                            MarcaComercial marcaComercial = await GetMarcaComercial(vacunaDesarrollada.IdMarcaComercial);
                            Dosis dosis = await GetDosis(item.IdDosis);

                            if (jurisdiccion.Id == usuarioExistente.IdJurisdiccion)
                            {
                                VacunaAplicadaConsultaDTO vacunaAplicadaDTO = new VacunaAplicadaConsultaDTO(item.Dni, item.Apellido, item.Nombre, item.FechaVacunacion, jurisdiccion.Descripcion, item.IdLote, lote.IdVacunaDesarrollada, vacuna.Descripcion, marcaComercial.Descripcion, dosis.Descripcion);
                                vacunasAplicadasDTO.Add(vacunaAplicadaDTO);
                            }
                        }
                    }
                    if (rolUsuario.Descripcion == "Operador Nacional")
                    {
                        foreach (VacunaAplicada item in vacunasAplicadas)
                        {
                            Jurisdiccion jurisdiccion = await GetDescripcionJurisdiccion(item.IdJurisdiccion);
                            Lote lote = await GetLote(item.IdLote);
                            VacunaDesarrollada vacunaDesarrollada = await GetVacunaDesarrollada(lote.IdVacunaDesarrollada);
                            Vacuna vacuna = await GetVacuna(vacunaDesarrollada.IdVacuna);
                            MarcaComercial marcaComercial = await GetMarcaComercial(vacunaDesarrollada.IdMarcaComercial);
                            Dosis dosis = await GetDosis(item.IdDosis);

                            VacunaAplicadaConsultaDTO vacunaAplicadaDTO = new VacunaAplicadaConsultaDTO(item.Dni, item.Apellido, item.Nombre, item.FechaVacunacion, jurisdiccion.Descripcion, item.IdLote, lote.IdVacunaDesarrollada, vacuna.Descripcion, marcaComercial.Descripcion, dosis.Descripcion);
                            vacunasAplicadasDTO.Add(vacunaAplicadaDTO);
                        }
                    }
                }

                responseListaVacunasAplicadas.EstadoTransaccion = transaccion;
                responseListaVacunasAplicadas.Errores = errores;
                responseListaVacunasAplicadas.ExistenciaErrores = existenciaErrores;
                responseListaVacunasAplicadas.EmailUsuario = emailUsuario;
                responseListaVacunasAplicadas.ListaVacunasAplicadasDTO = vacunasAplicadasDTO;

                return Ok(responseListaVacunasAplicadas);
            }
            catch (Exception error)
            {
                return BadRequest(error.Message);
            }
        }

        // GET: api/VacunasAplicadas/GetCantidadResultados?descripcionJurisdiccion=Buenos Aires
        [HttpGet]
        [Route("GetCantidadResultados")]
        public async Task<ActionResult<int>> GetCantidadResultados(string descripcionJurisdiccion = null)
        {
            try
            {
                List<VacunaAplicada> vacunasAplicadas = await _context.VacunaAplicada.ToListAsync();
                
                if (descripcionJurisdiccion != null)
                {
                    Jurisdiccion juris = await GetJurisdiccionByDescripcion(descripcionJurisdiccion);
                    vacunasAplicadas = vacunasAplicadas.Where(v => v.IdJurisdiccion == juris.Id).ToList();
                }

                return vacunasAplicadas.Count;
            }
            catch (Exception error)
            {
                return BadRequest(error.Message);
            }
        }

        // GET: api/VacunasAplicadas/GetVacunasAplicadas?emailOperadorNacional=juan@gmail.com?idJurisdiccion=2
        [HttpGet]
        [Route("GetVacunasAplicadas")]
        public async Task<ActionResult<IEnumerable<ResponseListaVacunasAplicadasDTO>>> GetVacunasAplicadas(string emailOperadorNacional = null, int idJurisdiccion = 0)
        {
            try
            {
                ResponseListaVacunasAplicadasDTO responseListaVacunasAplicadas = new ResponseListaVacunasAplicadasDTO();

                //lista vacia para los errores
                List<string> errores = new List<string>();
                List<VacunaAplicadaConsultaDTO> vacunasAplicadasDTO = new List<VacunaAplicadaConsultaDTO>();
                bool existenciaErrores = true;
                string transaccion = "Rechazada";

                errores = await VerificarCredencialesOperadorNacional(emailOperadorNacional, errores);

                //obtengo la lista
                List<VacunaAplicada> vacunasAplicadas = await _context.VacunaAplicada.ToListAsync();

                if (vacunasAplicadas.Count == 0)
                {
                    errores.Add("No existen vacunados en la base de datos");
                }

                if (idJurisdiccion == 0)
                {
                    errores.Add("No se especifica jurisdicción. Envíe el id de la jurisdicción deseada");
                }

                //obtengo la jurisdiccion enviada por parametro
                Jurisdiccion jurisdiccion = await GetDescripcionJurisdiccion(idJurisdiccion);

                if (jurisdiccion == null)
                {
                    errores.Add(String.Format("La jurisdicción con identificador {0} no existe en el sistema", idJurisdiccion));
                }

                if (errores.Count == 0)
                {
                    existenciaErrores = false;
                    transaccion = "Aceptada";

                    foreach (VacunaAplicada item in vacunasAplicadas)
                    {
                        Lote lote = await GetLote(item.IdLote);
                        VacunaDesarrollada vacunaDesarrollada = await GetVacunaDesarrollada(lote.IdVacunaDesarrollada);
                        Vacuna vacuna = await GetVacuna(vacunaDesarrollada.IdVacuna);
                        MarcaComercial marcaComercial = await GetMarcaComercial(vacunaDesarrollada.IdMarcaComercial);
                        Dosis dosis = await GetDosis(item.IdDosis);

                        if (idJurisdiccion == item.IdJurisdiccion)
                        {
                            VacunaAplicadaConsultaDTO vacunaAplicadaDTO = new VacunaAplicadaConsultaDTO(item.Dni, item.Apellido, item.Nombre, item.FechaVacunacion, jurisdiccion.Descripcion, item.IdLote, lote.IdVacunaDesarrollada, vacuna.Descripcion, marcaComercial.Descripcion, dosis.Descripcion);
                            vacunasAplicadasDTO.Add(vacunaAplicadaDTO);
                        }
                    }
                }

                responseListaVacunasAplicadas.EstadoTransaccion = transaccion;
                responseListaVacunasAplicadas.Errores = errores;
                responseListaVacunasAplicadas.ExistenciaErrores = existenciaErrores;
                responseListaVacunasAplicadas.EmailUsuario = emailOperadorNacional;
                responseListaVacunasAplicadas.ListaVacunasAplicadasDTO = vacunasAplicadasDTO;

                return Ok(responseListaVacunasAplicadas);
            }
            catch (Exception error)
            {
                return BadRequest(error.Message);
            }
        }

        // GET: api/VacunasAplicadas/5
        [HttpGet("{id}")]
        public async Task<ActionResult<VacunaAplicada>> GetVacunaAplicada(int id)
        {
            var vacunaAplicada = await _context.VacunaAplicada.FindAsync(id);

            if (vacunaAplicada == null)
            {
                return NotFound();
            }

            return vacunaAplicada;
        }

        // PUT: api/VacunasAplicadas/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutVacunaAplicada(int id, VacunaAplicada vacunaAplicada)
        {
            if (id != vacunaAplicada.Id)
            {
                return BadRequest();
            }

            _context.Entry(vacunaAplicada).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VacunaAplicadaExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/VacunasAplicadas/CrearVacunacion
        [HttpPost]
        [Route("CrearVacunacion")]
        public async Task<ActionResult<ResponseCrearVacunaAplicadaDTO>> CrearVacunacion([FromBody] RequestCrearVacunaAplicadaDTO model)
        {
            try
            {
                ResponseCrearVacunaAplicadaDTO responseCrearVacunaAplicadaDTO = null;
                List<string> errores = new List<string>();
                UsuarioRenaperDTO usuarioRenaper = await GetUsuarioRenaper(model.Dni);

                if (usuarioRenaper == null)
                    errores.Add(string.Format("El dni ingresado {0} no está registrado en Renaper", model.Dni));

                Distribucion distribucion = await _context.Distribucion.Where(d => d.IdLote == model.IdLote).FirstOrDefaultAsync();
                if (distribucion == null)
                    errores.Add(string.Format("No se ha podido localizar una distribución con identificador de lote {0}", model.IdLote));
                else if (distribucion.Vencidas > 0)
                    errores.Add(string.Format("El lote con identificador {0} tiene las vacunas vencidas", model.IdLote));
                else if (distribucion.Aplicadas == distribucion.CantidadVacunas)
                    errores.Add(string.Format("El lote con identificador {0} no tiene vacunas disponibles", model.IdLote));

                errores = await VerificarCredencialesUsuarioVacunador(model.EmailVacunador, errores);

                Vacuna vacunaExistente = await GetVacuna(model.IdVacuna);
                if (vacunaExistente == null)
                    errores.Add(string.Format("La vacuna con identificador {0} no está registrada en el sistema", model.IdVacuna));

                Dosis dosisExistente = await GetDosis(model.IdDosis);
                if (dosisExistente == null)
                    errores.Add(string.Format("La dosis con identificador {0} no está registrada en el sistema", model.IdDosis));

                Regla reglaExistente = await GetRegla(model.IdRegla);
                if (reglaExistente == null)
                    errores.Add(string.Format("La regla con identificador {0} no está registrada en el sistema", model.IdRegla));

                Lote loteExistente = await GetLote(model.IdLote);
                if (loteExistente == null)
                    errores.Add(string.Format("El lote con identificador {0} no está registrado en el sistema", model.IdLote));

                VacunaDesarrollada vacunaDesarrolladaExistente = await GetVacunaDesarrollada(model.IdVacunaDesarrollada);
                if (vacunaDesarrolladaExistente == null)
                    errores.Add(string.Format("La vacuna desarrollada con identificador {0} no está registrada en el sistema", model.IdVacunaDesarrollada));

                Jurisdiccion jurisdiccionExistente = await GetJurisdiccion(model.JurisdiccionResidencia);
                if (jurisdiccionExistente == null)
                    errores.Add(string.Format("La jurisdicción {0} no está registrada en el sistema", model.JurisdiccionResidencia));

                EntidadVacunaDosis entidadVacunaDosisExistente = await GetEntidadVacunaDosis(model.IdVacuna, model.IdDosis);
                if (entidadVacunaDosisExistente == null)
                    errores.Add(string.Format("La dosis con identificador {0} no está asociada a la vacuna con identificador {1}", model.IdDosis, model.IdVacuna));

                if (errores.Count > 0)
                    responseCrearVacunaAplicadaDTO = new ResponseCrearVacunaAplicadaDTO("Rechazada", true, errores, 0, model, null, null, null, null);
                else
                {
                    VacunaAplicada vacunaAplicada = new VacunaAplicada();
                    vacunaAplicada.Nombre = model.Nombre;
                    vacunaAplicada.Apellido = model.Apellido;
                    vacunaAplicada.Dni = model.Dni;
                    vacunaAplicada.Embarazada = model.Embarazada;
                    vacunaAplicada.FechaHoraNacimiento = model.FechaHoraNacimiento;
                    vacunaAplicada.FechaVacunacion = DateTime.Now;
                    vacunaAplicada.IdDosis = model.IdDosis;
                    vacunaAplicada.IdJurisdiccion = (await GetUsuario(model.EmailVacunador)).IdJurisdiccion.Value;
                    vacunaAplicada.IdJurisdiccionResidencia = jurisdiccionExistente.Id;
                    vacunaAplicada.IdLote = model.IdLote;
                    vacunaAplicada.PersonalSalud = model.PersonalSalud;
                    vacunaAplicada.SexoBiologico = model.SexoBiologico;
                    vacunaAplicada.Departamento = usuarioRenaper.ciudad;
                    vacunaAplicada.EnviadoDw = false;
                    distribucion.Aplicadas += 1;

                    _context.VacunaAplicada.Add(vacunaAplicada);
                    _context.Entry(distribucion).State = EntityState.Modified;
                    await _context.SaveChangesAsync();

                    //Agregación a tabla PendienteEnvioDW
                    PendienteEnvioDw pendienteEnvioDW = new PendienteEnvioDw();
                    pendienteEnvioDW.IdVacunaAplicada = vacunaAplicada.Id;
                    _context.PendienteEnvioDw.Add(pendienteEnvioDW);
                    await _context.SaveChangesAsync();

                    MarcaComercial mc = await _context.MarcaComercial.Where(m => m.Id == vacunaDesarrolladaExistente.IdMarcaComercial).FirstOrDefaultAsync();

                    responseCrearVacunaAplicadaDTO = new ResponseCrearVacunaAplicadaDTO("Aceptada", false, errores, vacunaAplicada.Id, model,
                        vacunaExistente.Descripcion, dosisExistente.Descripcion, reglaExistente.Descripcion, vacunaExistente.Descripcion + " " + mc.Descripcion);
                }

                return Ok(responseCrearVacunaAplicadaDTO);
            }
            catch (Exception error)
            {
                return BadRequest(error.Message);
            }
        }

        // GET: api/VacunasAplicadas/GenerarCsv?emailOperadorNacional=juan@gmail.com
        //[HttpGet]
        //[Route("GenerarCsv")]
        //public async Task<ActionResult<ResponseCargarVacunaDTO>> GenerarCsv(string emailOperadorNacional = null)
        //{
        //    try
        //    {
        //        ResponseCargarVacunaDTO response;
        //        List<string> errores = new List<string>();

        //        if (emailOperadorNacional == null)
        //        {
        //            errores.Add(string.Format("El email operador nacional es obligatorio"));
        //        }
        //        else
        //        {
        //            errores = await VerificarCredencialesUsuarioOperadorNacionalVacunador(emailOperadorNacional, errores);
        //        }

        //        if (errores.Count > 0)
        //            response = new ResponseCargarVacunaDTO("Rechazada", true, errores, emailOperadorNacional);
        //        else
        //        {
        //            List<UsuarioRenaperDTO> usuarios = new List<UsuarioRenaperDTO>();
        //            int id = 1;
        //            string separador = ",";
        //            List<string> tiposVacunas = new List<string>() { "arnn", "vector_viral", "subunidades_proteicas" };
        //            List<string> vacunasArnn = new List<string>();
        //            List<string> vacunasVectorViral = new List<string>();
        //            List<string> vacunasSubunidadesProteicas = new List<string>();
        //            DataWareHouseService servicio = new DataWareHouseService();

        //            StringBuilder salidaDLugar = new StringBuilder();
        //            StringBuilder salidaDTiempo = new StringBuilder();
        //            StringBuilder salidaDVacuna = new StringBuilder();
        //            StringBuilder salidaDVacunado = new StringBuilder();

        //            string rutaDLugar = @"F:\D_lugar.csv";
        //            string rutaDTiempo = @"F:\D_tiempo.csv";
        //            string rutaDVacuna = @"F:\D_vacuna.csv";
        //            string rutaDVacunado = @"F:\D_vacunado.csv";

        //            string lineaDLugar = "id,provincia,departamentos";
        //            string lineaDTiempo = "id,dia,mes,anio";
        //            string lineaDVacuna = "id,Laboratorio,Tipo_vacuna_desarrollada,id_lote,Vacuna_desarrollada";
        //            string lineaDVacunado = "id,anio,decadas,veintenas,sexo_biologico,dni";

        //            salidaDLugar.AppendLine(string.Join(separador, lineaDLugar));
        //            salidaDTiempo.AppendLine(string.Join(separador, lineaDTiempo));
        //            salidaDVacuna.AppendLine(string.Join(separador, lineaDVacuna));
        //            salidaDVacunado.AppendLine(string.Join(separador, lineaDVacunado));

        //            usuarios = await GetUsuarios(30000000, 31000000);

        //            foreach (UsuarioRenaperDTO usuarioRenaper in usuarios)
        //            {
        //                List<string> tipoVacuna = GenerarCsvService.GetTipoVacuna(tiposVacunas, vacunasArnn, vacunasVectorViral, vacunasSubunidadesProteicas, usuarioRenaper.vacunas);

        //                if (tipoVacuna[1] == "No existe")
        //                {
        //                    switch (tipoVacuna[0])
        //                    {
        //                        case "arnn":
        //                            vacunasArnn.Add(usuarioRenaper.vacunas);
        //                            break;
        //                        case "vector_viral":
        //                            vacunasVectorViral.Add(usuarioRenaper.vacunas);
        //                            break;
        //                        case "subunidades_proteicas":
        //                            vacunasSubunidadesProteicas.Add(usuarioRenaper.vacunas);
        //                            break;
        //                        default:
        //                            break;
        //                    }
        //                }

        //                string[] vacunas = usuarioRenaper.vacunas.Split("_");
        //                int anioNac = Convert.ToDateTime(usuarioRenaper.fecha_hora_nacimiento).Year;
        //                int anioHoy = DateTime.Now.Year;
        //                int decadas = (anioHoy - anioNac) / 10;
        //                int veintenas = (anioHoy - anioNac) / 20;

        //                lineaDLugar = id + "," + usuarioRenaper.jurisdiccion + "," + usuarioRenaper.ciudad;
        //                salidaDLugar.AppendLine(string.Join(separador, lineaDLugar));

        //                lineaDTiempo = id + "," + Convert.ToDateTime(usuarioRenaper.fecha_vacunacion).Day + "," + Convert.ToDateTime(usuarioRenaper.fecha_vacunacion).Month + "," + Convert.ToDateTime(usuarioRenaper.fecha_vacunacion).Year;
        //                salidaDTiempo.AppendLine(string.Join(separador, lineaDTiempo));

        //                lineaDVacuna = id + "," + vacunas[1] + "," + tipoVacuna[0] + "," + usuarioRenaper.lotes + "," + vacunas[0];
        //                salidaDVacuna.AppendLine(string.Join(separador, lineaDVacuna));

        //                lineaDVacunado = id + "," + anioNac + "," + decadas + "," + veintenas + "," + usuarioRenaper.genero + "," + usuarioRenaper.DNI;
        //                salidaDVacunado.AppendLine(string.Join(separador, lineaDVacunado));

        //                id++;

        //                //await servicio.CargarDataWareHouse2(_context2, usuarioRenaper, vacunas, tipoVacuna[0]);
        //            }

        //            GenerarCsvService.GenerarCsv(rutaDLugar, salidaDLugar);
        //            GenerarCsvService.GenerarCsv(rutaDTiempo, salidaDTiempo);
        //            GenerarCsvService.GenerarCsv(rutaDVacuna, salidaDVacuna);
        //            GenerarCsvService.GenerarCsv(rutaDVacunado, salidaDVacunado);

        //            response = new ResponseCargarVacunaDTO("Aceptada", true, errores, emailOperadorNacional);
        //        }

        //        return Ok(response);
        //    }
        //    catch (Exception error)
        //    {
        //        return BadRequest(error.Message);
        //    }
        //}

        // GET: api/VacunasAplicadas/CargarNuevosVacunadosApi?emailOperadorNacional=juan@gmail.com
        [HttpPost]
        [Route("CargarNuevosVacunadosApi")]
        public async Task<ActionResult<ResponseCargarVacunaDTO>> CargarNuevosVacunadosApi(string emailOperadorNacional = null)
        {
            try
            {
                ResponseCargarVacunaDTO response;
                List<string> errores = new List<string>();

                if (emailOperadorNacional == null)
                {
                    errores.Add(string.Format("El email operador nacional es obligatorio"));
                }
                else
                {
                    errores = await VerificarCredencialesUsuarioOperadorNacionalVacunador(emailOperadorNacional, errores);
                }

                if (errores.Count > 0)
                    response = new ResponseCargarVacunaDTO("Rechazada", true, errores, emailOperadorNacional);
                else
                {
                    List<UsuarioRenaperDTO> usuarios = new List<UsuarioRenaperDTO>();
                    List<VacunaAplicada> aplicadas = new List<VacunaAplicada>();
                    List<string> tiposVacunas = new List<string>() { "arnn", "vector_viral", "subunidades_proteicas" };
                    List<string> vacunasArnn = new List<string>();
                    List<string> vacunasVectorViral = new List<string>();
                    List<string> vacunasSubunidadesProteicas = new List<string>();
                    List<LoteJurisdiccionDTO> lotesJurisdicciones = new List<LoteJurisdiccionDTO>();
                    bool existeLote = false;
                    int totalAplicadas = 0;
                    DataWareHouseService servicio = new DataWareHouseService();

                    usuarios = await GetUsuarios(31000000, 31001000);

                    foreach (UsuarioRenaperDTO usuarioRenaper in usuarios)
                    {
                        //List<string> tipoVacuna = GenerarCsvService.GetTipoVacuna(tiposVacunas, vacunasArnn, vacunasVectorViral, vacunasSubunidadesProteicas, usuarioRenaper.vacunas);
                        List<string> tipoVacuna = new List<string>() { "arnn" };

                        if (tipoVacuna[1] == "No existe")
                        {
                            switch (tipoVacuna[0])
                            {
                                case "arnn":
                                    vacunasArnn.Add(usuarioRenaper.vacunas);
                                    break;
                                case "vector_viral":
                                    vacunasVectorViral.Add(usuarioRenaper.vacunas);
                                    break;
                                case "subunidades_proteicas":
                                    vacunasSubunidadesProteicas.Add(usuarioRenaper.vacunas);
                                    break;
                                default:
                                    break;
                            }
                        }

                        string[] vacunas = usuarioRenaper.vacunas.Split("_");
                        int anioNac = Convert.ToDateTime(usuarioRenaper.fecha_hora_nacimiento).Year;
                        int anioHoy = DateTime.Now.Year;
                        int decadas = (anioHoy - anioNac) / 10;
                        int veintenas = (anioHoy - anioNac) / 20;

                        foreach (LoteJurisdiccionDTO loteJuris in lotesJurisdicciones)
                        {
                            if (loteJuris.Lote == usuarioRenaper.lotes)
                            {
                                existeLote = true;
                                break;
                            }
                        }

                        Jurisdiccion jurisdiccion = await GetJurisdiccion(usuarioRenaper.jurisdiccion);

                        if (!existeLote)
                        {
                            LoteJurisdiccionDTO loteJuris = new LoteJurisdiccionDTO(usuarioRenaper.lotes, vacunas[0], vacunas[1], tipoVacuna[0]);
                            JurisdiccionVacunaAplicadaDTO jurisdiccionDTO = new JurisdiccionVacunaAplicadaDTO(jurisdiccion.Id, jurisdiccion.Descripcion, 0);
                            loteJuris.JurisdiccionesADistribuir.Add(jurisdiccionDTO);
                            lotesJurisdicciones.Add(loteJuris);
                        }

                        foreach (LoteJurisdiccionDTO lote in lotesJurisdicciones)
                        {
                            foreach (JurisdiccionVacunaAplicadaDTO jva in lote.JurisdiccionesADistribuir)
                            {
                                if (jva.IdJurisdiccion == jurisdiccion.Id)
                                {
                                    jva.CantidadAplicadas++;
                                    totalAplicadas++;
                                    break;
                                }
                            }
                        }

                        VacunaAplicada vacunaAplicada = new VacunaAplicada();
                        vacunaAplicada.Nombre = usuarioRenaper.nombre;
                        vacunaAplicada.Apellido = usuarioRenaper.apellido;
                        vacunaAplicada.Dni = usuarioRenaper.DNI;
                        vacunaAplicada.Embarazada = usuarioRenaper.embarazada;
                        vacunaAplicada.FechaHoraNacimiento = Convert.ToDateTime(usuarioRenaper.fecha_hora_nacimiento);
                        vacunaAplicada.FechaVacunacion = DateTime.Now;
                        vacunaAplicada.IdDosis = 0;
                        vacunaAplicada.IdJurisdiccion = jurisdiccion.Id;
                        vacunaAplicada.IdJurisdiccionResidencia = jurisdiccion.Id;
                        vacunaAplicada.IdLote = usuarioRenaper.lotes;
                        vacunaAplicada.PersonalSalud = usuarioRenaper.personal_salud;
                        vacunaAplicada.SexoBiologico = usuarioRenaper.genero;
                        vacunaAplicada.Departamento = usuarioRenaper.ciudad;
                        vacunaAplicada.EnviadoDw = false;

                        aplicadas.Add(vacunaAplicada);

                        //_context.VacunaAplicada.Add(vacunaAplicada);
                        //_context.Entry(distribucion).State = EntityState.Modified;
                        //await _context.SaveChangesAsync();

                        ////Agregación a tabla PendienteEnvioDW
                        //PendienteEnvioDw pendienteEnvioDW = new PendienteEnvioDw();
                        //pendienteEnvioDW.IdVacunaAplicada = vacunaAplicada.Id;
                        //_context.PendienteEnvioDw.Add(pendienteEnvioDW);
                        //await _context.SaveChangesAsync();

                        //await servicio.CargarDataWareHouse2(_context2, usuarioRenaper, vacunas, tipoVacuna[0]);
                        existeLote = false;
                    }

                    foreach (LoteJurisdiccionDTO l in lotesJurisdicciones)
                    {
                        EstadoCompra estadoCompra = await _context.EstadoCompra.Where(ec => ec.Descripcion == "Recibida").FirstOrDefaultAsync();

                        //Alta marca comercial
                        MarcaComercial mc = await GetMarcaComercialByDescripcion(l.Laboratorio);
                        if (mc == null)
                        {
                            mc = new MarcaComercial();
                            mc.Descripcion = l.Laboratorio;
                            _context.MarcaComercial.Add(mc);
                            await _context.SaveChangesAsync();
                        }

                        //Alta de vacuna
                        Vacuna vacuna = await GetVacunaByDescripcion(l.VacunaDesarrollada);
                        if (vacuna == null)
                        {
                            vacuna = new Vacuna();
                            vacuna.Descripcion = l.VacunaDesarrollada;
                            vacuna.IdPandemia = 0;
                            vacuna.IdTipoVacuna = 0;
                            vacuna.CantidadDosis = 0;
                            _context.Vacuna.Add(vacuna);
                            await _context.SaveChangesAsync();
                        }

                        //AltaVacuna desarrollada
                        VacunaDesarrollada vacunaDesarrollada = new VacunaDesarrollada();
                        vacunaDesarrollada.IdMarcaComercial = mc.Id;
                        vacunaDesarrollada.IdVacuna = vacuna.Id;
                        vacunaDesarrollada.DiasDemoraEntrega = 0;
                        vacunaDesarrollada.PrecioVacuna = 2000;
                        vacunaDesarrollada.TipoVacunaDesarrollada = l.TipoVacunaDesarrollada;
                        vacunaDesarrollada.FechaDesde = DateTime.Now;
                        vacunaDesarrollada.FechaHasta = null;
                        _context.VacunaDesarrollada.Add(vacunaDesarrollada);
                        await _context.SaveChangesAsync();

                        //Alta de compra
                        Random randomCodigoCompra = new Random();
                        int codigoCompra = randomCodigoCompra.Next(1, 100000000);

                        while (await GetCompraExistente(codigoCompra) != null)
                        {
                            codigoCompra = randomCodigoCompra.Next(1, 100000000);
                        }

                        Lote lote = new Lote(vacunaDesarrollada.Id, DateTime.Now.AddDays(365));
                        lote.Disponible = true;
                        _context.Lote.Add(lote);
                        await _context.SaveChangesAsync();
                        lote.Id = l.Lote;
                        _context.Entry(lote).State = EntityState.Modified;
                        await _context.SaveChangesAsync();

                        Compra compra = new Compra(lote.Id, estadoCompra.Id, (l.JurisdiccionesADistribuir.Count*10000), codigoCompra, DateTime.Now);
                        _context.Compra.Add(compra);
                        await _context.SaveChangesAsync();

                        //Alta de distribuciones
                        foreach (JurisdiccionVacunaAplicadaDTO juris in l.JurisdiccionesADistribuir)
                        {
                            Distribucion distribucion = new Distribucion(codigoCompra, juris.IdJurisdiccion, lote.Id, DateTime.Now, 10000, juris.CantidadAplicadas, 0);
                            _context.Distribucion.Add(distribucion);
                            await _context.SaveChangesAsync();
                        }
                    }

                    foreach (VacunaAplicada vacAplic in aplicadas)
                    {
                        PendienteEnvioDw pendiente = new PendienteEnvioDw();
                        _context.VacunaAplicada.Add(vacAplic);
                        await _context.SaveChangesAsync();
                        pendiente.IdVacunaAplicada = vacAplic.Id;
                        _context.PendienteEnvioDw.Add(pendiente);
                        await _context.SaveChangesAsync();
                    }

                    response = new ResponseCargarVacunaDTO("Aceptada", true, errores, emailOperadorNacional);
                }

                return Ok(response);
            }
            catch (Exception error)
            {
                return BadRequest(error.Message);
            }
        }

        // GET: api/VacunasAplicadas/Etl?emailOperadorNacional=juan@gmail.com
        [HttpGet]
        [Route("Etl")]
        public async Task<ActionResult<ResponseCargarVacunaDTO>> Etl(string emailOperadorNacional = null)
        {
            try
            {
                ResponseCargarVacunaDTO response;
                List<string> errores = new List<string>();

                if (emailOperadorNacional == null)
                {
                    errores.Add(string.Format("El email operador nacional es obligatorio"));
                }
                else
                {
                    errores = await VerificarCredencialesUsuarioOperadorNacionalVacunador(emailOperadorNacional, errores);
                }

                if (errores.Count > 0)
                    response = new ResponseCargarVacunaDTO("Rechazada", true, errores, emailOperadorNacional);
                else
                {
                    List<PendienteEnvioDw> pendientesEnviosDW = await _context.PendienteEnvioDw.ToListAsync();

                    foreach (PendienteEnvioDw pendiente in pendientesEnviosDW)
                    {
                        VacunaAplicada vacunaAplicada = await _context.VacunaAplicada.Where(v => v.Id == pendiente.IdVacunaAplicada).FirstOrDefaultAsync();

                        if (vacunaAplicada != null)
                        {
                            DataWareHouseService serviceDW = new DataWareHouseService();
                            Jurisdiccion jurisdiccion = await GetDescripcionJurisdiccion(vacunaAplicada.IdJurisdiccionResidencia);
                            Lote lote = await GetLote(vacunaAplicada.IdLote);
                            VacunaDesarrollada vacunaDesarrollada = await GetVacunaDesarrollada(lote.IdVacunaDesarrollada);
                            MarcaComercial mc = await GetMarcaComercial(vacunaDesarrollada.IdMarcaComercial);
                            Vacuna vacuna = await GetVacuna(vacunaDesarrollada.IdVacuna);
                            bool valido = await serviceDW.CargarDataWareHouse(_context2, vacunaAplicada, jurisdiccion.Descripcion, vacuna.Descripcion, mc.Descripcion, vacunaDesarrollada.TipoVacunaDesarrollada, lote.Id);

                            if (valido)
                            {
                                _context.PendienteEnvioDw.Remove(pendiente);
                                vacunaAplicada.EnviadoDw = true;
                                _context.Entry(vacunaAplicada).State = EntityState.Modified;
                                await _context.SaveChangesAsync();
                            }
                        }
                    }

                    response = new ResponseCargarVacunaDTO("Aceptada", true, errores, emailOperadorNacional);
                }

                return Ok(response);
            }
            catch (Exception error)
            {
                return BadRequest(error.Message);
            }
        }

        // POST: api/VacunasAplicadas/ConsultarVacunacion
        [HttpPost]
        [Route("ConsultarVacunacion")]
        public async Task<ActionResult<ResponseVacunaAplicadaDTO>> ConsultarVacunacion([FromBody] RequestVacunaAplicadaDTO model)
        {
            try
            {
                ResponseVacunaAplicadaDTO responseVacunaAplicadaDTO = null;
                List<string> errores = new List<string>();

                errores = await VerificarCredencialesUsuarioVacunador(model.EmailVacunador, errores);

                if (await GetUsuarioRenaper(model.Dni) == null)
                    errores.Add(string.Format("El dni ingresado {0} no está registrado en Renaper", model.Dni));

                Vacuna vacunaExistente = await GetVacuna(model.IdVacuna);
                if (vacunaExistente == null)
                    errores.Add(string.Format("La vacuna con identificador {0} no está registrada en el sistema", model.IdVacuna));
                else
                {
                    VacunaDesarrolladaVacunacionDTO vacunaDesarrolladaDisponible = await GetVacunaDesarrolladaAplicacion(vacunaExistente, model.EmailVacunador);
                    if (vacunaDesarrolladaDisponible == null)
                        errores.Add(string.Format("No hay vacuna desarrollada en stock para la vacuna {0}", vacunaExistente.Descripcion));
                }

                Dosis dosisExistente = await GetDosis(model.IdDosis);
                if (dosisExistente == null)
                    errores.Add(string.Format("La dosis con identificador {0} no está registrada en el sistema", model.IdDosis));

                Jurisdiccion jurisdiccionExistente = await GetJurisdiccion(model.JurisdiccionResidencia);
                if (jurisdiccionExistente == null)
                    errores.Add(string.Format("La jurisdicción {0} no está registrada en el sistema", model.JurisdiccionResidencia));

                EntidadVacunaDosis entidadVacunaDosisExistente = await GetEntidadVacunaDosis(model.IdVacuna, model.IdDosis);
                if (entidadVacunaDosisExistente == null)
                    errores.Add(string.Format("La dosis con identificador {0} no está asociada a la vacuna con identificador {1}", model.IdDosis, model.IdVacuna));

                if (errores.Count == 0)
                {
                    TipoVacuna tipoVacuna = await _context.TipoVacuna.Where(tp => tp.Id == vacunaExistente.IdTipoVacuna).FirstOrDefaultAsync();

                    if (tipoVacuna.Descripcion == "Vacuna Anual")
                    {
                        EntidadDosisRegla edr = await _context.EntidadDosisRegla.Where(x => x.IdDosis == model.IdDosis).FirstOrDefaultAsync();
                        List<string> alertasVacunacion = new List<string>();
                        DateTime fechaActual = DateTime.Now;
                        int anioActual = fechaActual.Year;
                        int mesActual = Convert.ToInt32(fechaActual.Month.ToString());

                        if (edr != null)
                        {
                            Regla regla = await _context.Regla.Where(r => r.Id == edr.IdRegla).FirstOrDefaultAsync();

                            if (regla != null)
                            {
                                VacunaAplicada vacunaAplicadaAnual = await _context.VacunaAplicada
                                    .Where(va => va.Dni == model.Dni
                                        && va.IdDosis == model.IdDosis
                                        && va.FechaVacunacion.Year == anioActual)
                                    .FirstOrDefaultAsync();

                                if (vacunaAplicadaAnual == null)
                                {
                                    string[] meses = regla.MesesVacunacion.Split(",");
                                    List<int> listaMeses = new List<int>();

                                    foreach (string m in meses)
                                    {
                                        listaMeses.Add(Convert.ToInt32(m));
                                    }

                                    if (mesActual < listaMeses.First())
                                        alertasVacunacion.Add(string.Format("La vacuna anual {0} debe aplicarse a partir del mes {1} del año {2}",
                                            vacunaExistente.Descripcion, listaMeses.First(), anioActual));

                                    if (mesActual > listaMeses.Last())
                                        alertasVacunacion.Add(string.Format("Aplicación fuera de término: la vacuna anual {0} debe aplicarse hasta el mes {1} del año {2}",
                                            vacunaExistente.Descripcion, listaMeses.Last(), anioActual));

                                    ReglaDTO reglaDTO = new ReglaDTO(regla.Id, regla.Descripcion, regla.MesesVacunacion, regla.LapsoMinimoDias,
                                        regla.LapsoMaximoDias, regla.Otros, regla.Embarazada, regla.PersonalSalud);
                                    DosisDTO dosisDTO = new DosisDTO(dosisExistente.Id, entidadVacunaDosisExistente.Orden.Value,
                                        dosisExistente.Descripcion, new List<ReglaDTO> { reglaDTO });

                                    VacunaDesarrolladaVacunacionDTO vacunaDesarrolladaAplicacion = await GetVacunaDesarrolladaAplicacion(vacunaExistente, model.EmailVacunador);

                                    responseVacunaAplicadaDTO = new ResponseVacunaAplicadaDTO("Aceptada", false, errores, model, dosisDTO,
                                        alertasVacunacion, vacunaDesarrolladaAplicacion);
                                }
                                else
                                {
                                    alertasVacunacion.Add(string.Format("La vacuna anual {0} fue aplicada en la fecha {1}", vacunaExistente.Descripcion, vacunaAplicadaAnual.FechaVacunacion));
                                    responseVacunaAplicadaDTO = new ResponseVacunaAplicadaDTO("Aceptada", false, errores, model, null, alertasVacunacion, null);
                                }
                            }
                        }
                    }

                    if (tipoVacuna.Descripcion == "Vacuna de Calendario")
                    {
                        List<EntidadVacunaDosis> entidadesVD = await _context.EntidadVacunaDosis.Where(evd => evd.IdVacuna == vacunaExistente.Id).ToListAsync();
                        List<int> identificadoresDosis = new List<int>();
                        List<Dosis> dosisAplicadas = new List<Dosis>();
                        List<string> alertasVacunacion = new List<string>();
                        List<List<string>> datosProximaDosis = new List<List<string>>();

                        foreach (EntidadVacunaDosis evd in entidadesVD)
                        {
                            identificadoresDosis.Add(evd.IdDosis);
                        }

                        List<VacunaAplicada> vacunasAplicadas = await _context.VacunaAplicada
                            .Where(d => d.Dni == model.Dni)
                            .OrderBy(d => d.FechaVacunacion)
                            .ToListAsync();

                        foreach (VacunaAplicada va in vacunasAplicadas)
                        {
                            if (identificadoresDosis.Contains(va.IdDosis))
                            {
                                Dosis d = await _context.Dosis.Where(x => x.Id == va.IdDosis).FirstOrDefaultAsync();

                                if (d != null)
                                {
                                    dosisAplicadas.Add(d);
                                }
                            }
                        }

                        datosProximaDosis = await ObtenerDatosProximaDosis(model.FechaHoraNacimiento, dosisAplicadas, vacunaExistente.Descripcion, model.Embarazada, model.PersonalSalud, model.Dni);
                        alertasVacunacion = datosProximaDosis[1];

                        if (datosProximaDosis[0][0] == null)
                            responseVacunaAplicadaDTO = new ResponseVacunaAplicadaDTO("Aceptada", false, errores, model, null, alertasVacunacion, null);
                        else
                        {
                            Dosis proximaDosis = await _context.Dosis.Where(d => d.Descripcion == datosProximaDosis[0][0]).FirstOrDefaultAsync();
                            EntidadDosisRegla entDR = await _context.EntidadDosisRegla.Where(e => e.IdDosis == proximaDosis.Id).FirstOrDefaultAsync();
                            Regla regla = await _context.Regla.Where(r => r.Id == entDR.IdRegla).FirstOrDefaultAsync();
                            EntidadVacunaDosis evd = await _context.EntidadVacunaDosis.Where(e => e.IdDosis == proximaDosis.Id).FirstOrDefaultAsync();

                            ReglaDTO reglaDTO = new ReglaDTO(regla.Id, regla.Descripcion, regla.MesesVacunacion, regla.LapsoMinimoDias,
                                regla.LapsoMaximoDias, regla.Otros, regla.Embarazada, regla.PersonalSalud);
                            DosisDTO dosisDTO = new DosisDTO(proximaDosis.Id, evd.Orden.Value,
                                proximaDosis.Descripcion, new List<ReglaDTO> { reglaDTO });

                            VacunaDesarrolladaVacunacionDTO vacunaDesarrolladaAplicacion = await GetVacunaDesarrolladaAplicacion(vacunaExistente, model.EmailVacunador);

                            responseVacunaAplicadaDTO = new ResponseVacunaAplicadaDTO("Aceptada", false, errores, model, dosisDTO,
                                alertasVacunacion, vacunaDesarrolladaAplicacion);
                        }
                    }

                    if (tipoVacuna.Descripcion == "Vacuna de Pandemia")
                    {
                        Pandemia pandemia = await _context.Pandemia.Where(p => p.Descripcion == vacunaExistente.Descripcion).FirstOrDefaultAsync();
                        List<EntidadVacunaDosis> entidadesVD = await _context.EntidadVacunaDosis.Where(evd => evd.IdVacuna == model.IdVacuna).ToListAsync();
                        List<int> identificadoresDosis = new List<int>();
                        List<Dosis> dosisAplicadas = new List<Dosis>();
                        List<string> alertasVacunacion = new List<string>();
                        List<List<string>> datosProximaDosis = new List<List<string>>();
                        int ordenReferenciaDosis = 0;
                        Dosis proximaDosis = null;
                        Regla regla = null;

                        foreach (EntidadVacunaDosis evd in entidadesVD)
                        {
                            identificadoresDosis.Add(evd.IdDosis);
                        }

                        List<VacunaAplicada> vacunasAplicadas = await _context.VacunaAplicada
                           .Where(d => d.Dni == model.Dni)
                           .OrderBy(d => d.FechaVacunacion)
                           .ToListAsync();

                        foreach (VacunaAplicada va in vacunasAplicadas)
                        {
                            if (identificadoresDosis.Contains(va.IdDosis))
                            {
                                Dosis d = await _context.Dosis.Where(x => x.Id == va.IdDosis).FirstOrDefaultAsync();

                                if (d != null)
                                {
                                    dosisAplicadas.Add(d);
                                }
                            }
                        }

                        if (vacunasAplicadas.Count > 0)
                            ordenReferenciaDosis = dosisAplicadas.Count;

                        if (dosisAplicadas.Count != entidadesVD.Count)
                        {
                            EntidadVacunaDosis evdSig = await _context.EntidadVacunaDosis.Where(evd => evd.Orden == ordenReferenciaDosis && evd.IdVacuna == model.IdVacuna).FirstOrDefaultAsync();

                            if (evdSig != null)
                            {
                                if (pandemia.FechaFin < DateTime.Now)
                                {
                                    alertasVacunacion.Add(string.Format("La pandemia {0} ha finalizado en la fecha {1}", pandemia.Descripcion, pandemia.FechaFin));
                                }

                                proximaDosis = await _context.Dosis.Where(d => d.Id == evdSig.IdDosis).FirstOrDefaultAsync();
                                EntidadDosisRegla entDR = await _context.EntidadDosisRegla.Where(e => e.IdDosis == proximaDosis.Id).FirstOrDefaultAsync();
                                regla = await _context.Regla.Where(r => r.Id == entDR.IdRegla).FirstOrDefaultAsync();

                                ReglaDTO reglaDTO = new ReglaDTO(regla.Id, regla.Descripcion, regla.MesesVacunacion, regla.LapsoMinimoDias,
                                   regla.LapsoMaximoDias, regla.Otros, regla.Embarazada, regla.PersonalSalud);
                                DosisDTO dosisDTO = new DosisDTO(proximaDosis.Id, evdSig.Orden.Value,
                                    proximaDosis.Descripcion, new List<ReglaDTO> { reglaDTO });

                                if (dosisAplicadas.Count != 0)
                                {
                                    List<VacunaAplicada> aplicaciones = new List<VacunaAplicada>();

                                    foreach (int identificador in identificadoresDosis)
                                    {
                                        VacunaAplicada aplicacion = await _context.VacunaAplicada.Where(v => v.IdDosis == identificador).FirstOrDefaultAsync();

                                        if (aplicacion != null)
                                            aplicaciones.Add(aplicacion);
                                    }

                                    aplicaciones = aplicaciones.OrderBy(v => v.FechaVacunacion).ToList();

                                    if ((DateTime.Now - aplicaciones.Last().FechaVacunacion).TotalDays < regla.LapsoMinimoDias)
                                    {
                                        alertasVacunacion.Add(string.Format("No ha transcurrido el tiempo mínimo de {0} días desde la última dosis aplicada", regla.LapsoMinimoDias));
                                    }
                                }

                                VacunaDesarrolladaVacunacionDTO vacunaDesarrolladaAplicacion = await GetVacunaDesarrolladaAplicacion(vacunaExistente, model.EmailVacunador);
                                responseVacunaAplicadaDTO = new ResponseVacunaAplicadaDTO("Aceptada", false, errores, model, dosisDTO, alertasVacunacion, vacunaDesarrolladaAplicacion);
                            }
                        }
                        else
                        {
                            alertasVacunacion.Add(string.Format("Fueron aplicadas todas las dosis de vacuna para la pandemia {0}", pandemia.Descripcion));
                            responseVacunaAplicadaDTO = new ResponseVacunaAplicadaDTO("Aceptada", false, errores, model, null, alertasVacunacion, null);
                        }
                    }
                }
                else
                    responseVacunaAplicadaDTO = new ResponseVacunaAplicadaDTO("Rechazada", true, errores, model, null, null, null);

                return Ok(responseVacunaAplicadaDTO);
            }
            catch (Exception error)
            {
                return BadRequest(error.Message);
            }
        }

        // DELETE: api/VacunasAplicadas/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<VacunaAplicada>> DeleteVacunaAplicada(int id)
        {
            var vacunaAplicada = await _context.VacunaAplicada.FindAsync(id);
            if (vacunaAplicada == null)
            {
                return NotFound();
            }

            _context.VacunaAplicada.Remove(vacunaAplicada);
            await _context.SaveChangesAsync();

            return vacunaAplicada;
        }



        //Métodos privados de ayuda
        private async Task<Compra> GetCompraExistente(int codigoCompra)
        {
            Compra compraExistente = null;

            try
            {
                compraExistente = await _context.Compra
                    .Where(c => c.Codigo == codigoCompra).FirstOrDefaultAsync();
            }
            catch
            {

            }

            return compraExistente;
        }

        private async Task<UsuarioRenaperDTO> GetUsuarioRenaper(int dni)
        {
            UsuarioRenaperDTO usuario = null;

            try
            {
                var renaperUrl = "https://api.claudioraverta.com/personas/" + dni;

                using (var httpClient = new HttpClient())
                {
                    var respuesta = await httpClient.GetAsync(renaperUrl);

                    if (respuesta.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        var respuestaString = await respuesta.Content.ReadAsStringAsync();
                        usuario = JsonSerializer.Deserialize<UsuarioRenaperDTO>(respuestaString,
                            new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                        if (usuario == null || usuario.DNI != dni)
                            usuario = null;
                    }
                }
            }
            catch
            {

            }

            return usuario;
        }

        private async Task<List<UsuarioRenaperDTO>> GetUsuarios(int desde, int hasta)
        {
            List<UsuarioRenaperDTO> usuarios = new List<UsuarioRenaperDTO>();
            UsuarioRenaperDTO usuario = null;

            try
            {
                using (var httpClient = new HttpClient())
                {
                    for (int dni = desde; dni < hasta; dni++)
                    {
                        var respuesta = await httpClient.GetAsync("https://api.claudioraverta.com/personas/" + dni);

                        if (respuesta.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            var respuestaString = await respuesta.Content.ReadAsStringAsync();
                            usuario = JsonSerializer.Deserialize<UsuarioRenaperDTO>(respuestaString,
                                new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                            if (usuario != null)
                                usuarios.Add(usuario);
                        }
                    }
                }
            }
            catch
            {

            }

            return usuarios;
        }

        private bool VacunaAplicadaExists(int id)
        {
            return _context.VacunaAplicada.Any(e => e.Id == id);
        }

        private async Task<bool> TieneRolOperadorNacional(Usuario usuario)
        {
            try
            {
                Rol rolOperadorNacional = await _context.Rol
                    .Where(rol => rol.Descripcion == "Operador Nacional").FirstOrDefaultAsync();

                if (rolOperadorNacional.Id == usuario.IdRol)
                {
                    return true;
                }
            }
            catch
            {

            }
            return false;
        }

        private async Task<Usuario> CuentaUsuarioExists(string email)
        {
            Usuario cuentaExistente = null;
            try
            {
                cuentaExistente = await _context.Usuario
                    .Where(user => user.Email == email).FirstOrDefaultAsync();
            }
            catch
            {

            }
            return cuentaExistente;
        }

        private async Task<List<string>> VerificarCredencialesOperadorNacional(string emailOperadorNacional, List<string> errores)
        {
            try
            {
                Usuario usuarioSolicitante = await CuentaUsuarioExists(emailOperadorNacional);
                if (usuarioSolicitante != null)
                {
                    if (!await TieneRolOperadorNacional(usuarioSolicitante))
                    {
                        errores.Add(String.Format("El usuario {0} no tiene el rol de operador nacional", emailOperadorNacional));
                    }
                }
                else
                {
                    errores.Add(string.Format("El usuario {0} no existe en el sistema", emailOperadorNacional));
                }
            }
            catch
            {

            }

            return errores;
        }

        private async Task<Usuario> GetUsuario(string email)
        {
            try
            {
                Usuario cuentaExistente = new Usuario();
                List<Usuario> listaUsuarios = await _context.Usuario.ToListAsync();

                foreach (Usuario item in listaUsuarios)
                {
                    if (item.Email == email)
                    {
                        cuentaExistente.Id = item.Id;
                        cuentaExistente.Email = item.Email;
                        cuentaExistente.Password = item.Password;
                        cuentaExistente.IdJurisdiccion = item.IdJurisdiccion.Value;
                        cuentaExistente.IdRol = item.IdRol;
                    }
                }

                if (cuentaExistente.Email != null)
                    return cuentaExistente;
            }
            catch
            {

            }

            return null;
        }

        private async Task<Rol> GetRol(int idRol)
        {
            Rol rolExistente = null;

            try
            {
                rolExistente = await _context.Rol
                    .Where(rol => rol.Id == idRol).FirstOrDefaultAsync();
            }
            catch
            {

            }

            return rolExistente;
        }

        private async Task<Jurisdiccion> GetDescripcionJurisdiccion(int idJurisdiccion)
        {
            Jurisdiccion jurisdiccionExistente = null;

            try
            {
                jurisdiccionExistente = await _context.Jurisdiccion.Where(jurisdiccion => jurisdiccion.Id == idJurisdiccion).FirstOrDefaultAsync();
            }
            catch
            {

            }
            return jurisdiccionExistente;
        }

        private async Task<Jurisdiccion> GetJurisdiccionByDescripcion(string descripcionJurisdiccion)
        {
            Jurisdiccion jurisdiccionExistente = null;

            try
            {
                jurisdiccionExistente = await _context.Jurisdiccion.Where(jurisdiccion => jurisdiccion.Descripcion == descripcionJurisdiccion).FirstOrDefaultAsync();
            }
            catch
            {

            }
            return jurisdiccionExistente;
        }

        private async Task<Dosis> getDosis(int idDosis)
        {
            Dosis dosisExistente = null;

            try
            {
                dosisExistente = await _context.Dosis.Where(d => d.Id == idDosis).FirstOrDefaultAsync();
            }
            catch
            {

            }
            return dosisExistente;
        }

        private async Task<MarcaComercial> GetMarcaComercial(int idMarcaComercial)
        {
            MarcaComercial marcaComercialExistente = null;

            try
            {
                marcaComercialExistente = await _context.MarcaComercial.Where(mc => mc.Id == idMarcaComercial).FirstOrDefaultAsync();
            }
            catch
            {

            }
            return marcaComercialExistente;
        }

        private async Task<MarcaComercial> GetMarcaComercialByDescripcion(string marcaComercial)
        {
            MarcaComercial marcaComercialExistente = null;

            try
            {
                marcaComercialExistente = await _context.MarcaComercial.Where(mc => mc.Descripcion == marcaComercial).FirstOrDefaultAsync();
            }
            catch
            {

            }
            return marcaComercialExistente;
        }

        private async Task<bool> TieneRolVacunador(Usuario usuario)
        {
            try
            {
                Rol rolVacunador = await _context.Rol
                    .Where(rol => rol.Descripcion == "Vacunador").FirstOrDefaultAsync();

                if (rolVacunador.Id == usuario.IdRol)
                {
                    return true;
                }
            }
            catch
            {

            }

            return false;
        }

        private async Task<List<string>> VerificarCredencialesUsuarioVacunador(string emailVacunador, List<string> errores)
        {
            try
            {
                Usuario usuarioSolicitante = await GetUsuario(emailVacunador);
                if (usuarioSolicitante == null)
                    errores.Add(string.Format("El usuario {0} no está registrado en el sistema", emailVacunador));
                else
                {
                    bool tieneRolVacunador = await TieneRolVacunador(usuarioSolicitante);
                    if (!tieneRolVacunador)
                        errores.Add(string.Format("El usuario {0} no tiene rol vacunador", emailVacunador));
                }
            }
            catch
            {

            }

            return errores;
        }

        private async Task<EntidadVacunaDosis> GetEntidadVacunaDosis(int idVacuna, int idDosis)
        {
            EntidadVacunaDosis entidadVacunaDosisExistente = null;

            try
            {
                entidadVacunaDosisExistente = await _context.EntidadVacunaDosis
                    .Where(evd => evd.IdVacuna == idVacuna && evd.IdDosis == idDosis).FirstOrDefaultAsync();
            }
            catch
            {

            }

            return entidadVacunaDosisExistente;
        }

        private async Task<Jurisdiccion> GetJurisdiccion(string jurisdiccion)
        {
            Jurisdiccion jurisdiccionExistente = null;

            try
            {
                jurisdiccionExistente = await _context.Jurisdiccion
                    .Where(j => j.Descripcion == jurisdiccion).FirstOrDefaultAsync();
            }
            catch
            {

            }

            return jurisdiccionExistente;
        }

        private async Task<Lote> GetLote(int idLote)
        {
            Lote loteExistente = null;

            try
            {
                loteExistente = await _context.Lote
                    .Where(l => l.Id == idLote).FirstOrDefaultAsync();
            }
            catch
            {

            }

            return loteExistente;
        }

        private async Task<Vacuna> GetVacuna(int idVacuna)
        {
            Vacuna vacunaExistente = null;

            try
            {
                vacunaExistente = await _context.Vacuna
                    .Where(vac => vac.Id == idVacuna).FirstOrDefaultAsync();
            }
            catch
            {

            }

            return vacunaExistente;
        }

        private async Task<Vacuna> GetVacunaByDescripcion(string vacuna)
        {
            Vacuna vacunaExistente = null;

            try
            {
                vacunaExistente = await _context.Vacuna
                    .Where(vac => vac.Descripcion == vacuna).FirstOrDefaultAsync();
            }
            catch
            {

            }

            return vacunaExistente;
        }

        private async Task<VacunaDesarrollada> GetVacunaDesarrollada(int idVacunaDesarrollada)
        {
            VacunaDesarrollada vacunaDesarrolladaExistente = null;

            try
            {
                vacunaDesarrolladaExistente = await _context.VacunaDesarrollada
                    .Where(vac => vac.Id == idVacunaDesarrollada).FirstOrDefaultAsync();
            }
            catch
            {

            }

            return vacunaDesarrolladaExistente;
        }

        private async Task<Dosis> GetDosis(int idDosis)
        {
            Dosis dosisExistente = null;

            try
            {
                dosisExistente = await _context.Dosis
                    .Where(d => d.Id == idDosis).FirstOrDefaultAsync();
            }
            catch
            {

            }

            return dosisExistente;
        }

        private async Task<Regla> GetRegla(int idRegla)
        {
            Regla reglaExistente = null;

            try
            {
                reglaExistente = await _context.Regla
                    .Where(r => r.Id == idRegla).FirstOrDefaultAsync();
            }
            catch
            {

            }

            return reglaExistente;
        }

        private async Task<VacunaDesarrolladaVacunacionDTO> GetVacunaDesarrolladaAplicacion(Vacuna vacuna, string emailVacunador)
        {
            VacunaDesarrolladaVacunacionDTO vacunaDesarrolladaDTO = null;

            try
            {
                Usuario vacunador = await _context.Usuario.Where(u => u.Email == emailVacunador).FirstOrDefaultAsync();

                List<Distribucion> distribucionesJurisdiccionVacunadorNoVencidas = await _context.Distribucion
                    .Where(d => d.IdJurisdiccion == vacunador.IdJurisdiccion
                        && d.IdLoteNavigation.IdVacunaDesarrolladaNavigation.IdVacuna == vacuna.Id
                        && d.IdLoteNavigation.FechaVencimiento > DateTime.Now
                        && d.Vencidas == 0
                        && d.Aplicadas < d.CantidadVacunas)
                    .OrderBy(lote => lote.IdLoteNavigation.FechaVencimiento)
                    .ToListAsync();

                if (distribucionesJurisdiccionVacunadorNoVencidas.Count > 0)
                {
                    Distribucion distribucion = distribucionesJurisdiccionVacunadorNoVencidas.First();

                    Lote lote = await _context.Lote.Where(l => l.Id == distribucion.IdLote).FirstOrDefaultAsync();

                    VacunaDesarrollada vacunaDesarrollada = await _context.VacunaDesarrollada
                            .Where(vd => vd.Id == lote.IdVacunaDesarrollada)
                            .FirstOrDefaultAsync();

                    if (vacunaDesarrollada != null)
                    {
                        MarcaComercial marcaComercial = await _context.MarcaComercial.Where(mc => mc.Id == vacunaDesarrollada.IdMarcaComercial).FirstOrDefaultAsync();

                        if (marcaComercial != null)
                        {
                            vacunaDesarrolladaDTO = new VacunaDesarrolladaVacunacionDTO(vacunaDesarrollada.Id, vacuna.Descripcion + " " + marcaComercial.Descripcion, distribucion.IdLote);
                        }
                    }
                }
            }
            catch
            {

            }

            return vacunaDesarrolladaDTO;
        }

        private async Task<List<List<string>>> ObtenerDatosProximaDosis(DateTime fechaNacimiento, List<Dosis> dosisAplicadas, string descripcionVacuna, bool embarazada, bool personalSalud, int dni)
        {
            List<List<string>> listaProximasDosis = new List<List<string>>();

            try
            {
                switch (descripcionVacuna)
                {
                    case "Hepatitis B (HB)":
                        listaProximasDosis = await ObtenerProximaDosisHepatitisBHB(fechaNacimiento, dosisAplicadas, descripcionVacuna, embarazada, personalSalud, dni);
                        break;
                    case "BCG":
                        listaProximasDosis = ObtenerProximaDosisBCG(fechaNacimiento, dosisAplicadas, descripcionVacuna, embarazada, personalSalud);
                        break;
                    case "Rotavirus":
                        listaProximasDosis = ObtenerProximaDosisRotavirus(fechaNacimiento, dosisAplicadas, descripcionVacuna, embarazada, personalSalud);
                        break;
                    case "Neumococo Conjugada":
                        listaProximasDosis = ObtenerProximaDosisNeumococoConjugada(fechaNacimiento, dosisAplicadas, descripcionVacuna, embarazada, personalSalud);
                        break;
                    case "Quíntuple Pentavalente":
                        listaProximasDosis = ObtenerProximaDosisQuintuplePentavalente(fechaNacimiento, dosisAplicadas, descripcionVacuna, embarazada, personalSalud);
                        break;
                    case "Salk IPV":
                        listaProximasDosis = ObtenerProximaDosisSalkIPV(fechaNacimiento, dosisAplicadas, descripcionVacuna, embarazada, personalSalud);
                        break;
                    case "Meningocócica Conjugada":
                        listaProximasDosis = ObtenerProximaDosisMeningococicaConjugada(fechaNacimiento, dosisAplicadas, descripcionVacuna, embarazada, personalSalud);
                        break;
                    case "Triple Viral (SRP)":
                        listaProximasDosis = ObtenerProximaDosisTripleViralSRP(fechaNacimiento, dosisAplicadas, descripcionVacuna, embarazada, personalSalud);
                        break;
                    case "Hepatitis A (HA)":
                        listaProximasDosis = ObtenerProximaDosisHepatitisAHA(fechaNacimiento, dosisAplicadas, descripcionVacuna, embarazada, personalSalud);
                        break;
                    case "Varicela":
                        listaProximasDosis = ObtenerProximaDosisVaricela(fechaNacimiento, dosisAplicadas, descripcionVacuna, embarazada, personalSalud);
                        break;
                    case "Triple Bacteriana (DTP)":
                        listaProximasDosis = ObtenerProximaDosisTripleBacterianaDTP(fechaNacimiento, dosisAplicadas, descripcionVacuna, embarazada, personalSalud);
                        break;
                    case "Triple Bacteriana Acelular":
                        listaProximasDosis = await ObtenerProximaDosisTripleBacterianaAcelular(fechaNacimiento, dosisAplicadas, descripcionVacuna, embarazada, personalSalud, dni);
                        break;
                    case "VPH":
                        listaProximasDosis = await ObtenerProximaDosisVPH(fechaNacimiento, dosisAplicadas, descripcionVacuna, embarazada, personalSalud, dni);
                        break;
                    case "Doble Bacteriana (DT)":
                        listaProximasDosis = await ObtenerProximaDosisDobleBacterianaDT(fechaNacimiento, dosisAplicadas, descripcionVacuna, embarazada, personalSalud, dni);
                        break;
                    case "Doble Viral (SR)":
                        listaProximasDosis = ObtenerProximaDosisDobleViralSR(fechaNacimiento, dosisAplicadas, descripcionVacuna, embarazada, personalSalud);
                        break;
                    case "Fiebre Amarilla (FA)":
                        listaProximasDosis = await ObtenerProximaDosisFiebreAmarillaFA(fechaNacimiento, dosisAplicadas, descripcionVacuna, embarazada, personalSalud, dni);
                        break;
                    case "Fiebre Hemorrágica Argentina (FHA)":
                        listaProximasDosis = ObtenerProximaDosisFiebreHemorragicaArgentinaFHA(fechaNacimiento, dosisAplicadas, descripcionVacuna, embarazada, personalSalud);
                        break;
                    default:
                        break;
                }
            }
            catch
            {

            }

            return listaProximasDosis;
        }

        public async Task<List<List<string>>> ObtenerProximaDosisHepatitisBHB(DateTime fechaNacimiento, List<Dosis> dosisAplicadas, string descripcionVacuna, bool embarazada, bool personalSalud, int dni)
        {
            string proximaDosis = null;
            List<string> listaProximasDosis = new List<string>();
            List<string> alertasVacunacion = new List<string>();
            List<List<string>> listaResultado = new List<List<string>>();

            try
            {
                if (dosisAplicadas.Count == 0)
                {
                    double totalHoras = (DateTime.Now - fechaNacimiento).TotalHours;

                    if (totalHoras < 12)
                    {
                        proximaDosis = string.Format("{0} - Primera Dosis Nacimiento", descripcionVacuna);
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays < 4015)
                    {
                        alertasVacunacion.Add("La dosis debe aplicarse en las primeras 12 horas de vida");
                        proximaDosis = string.Format("{0} - Primera Dosis Nacimiento", descripcionVacuna);
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 4015)
                    {
                        proximaDosis = string.Format("{0} - Primera Dosis", descripcionVacuna);
                    }
                }
                else
                {
                    Dosis ultimaDosisAplicada = dosisAplicadas.Last();
                    Dosis primeraDosisAplicada = dosisAplicadas.First();

                    VacunaAplicada ultimaVacunaAplicada = await _context.VacunaAplicada
                        .Where(va => va.IdDosis == ultimaDosisAplicada.Id
                            && va.Dni == dni)
                        .FirstOrDefaultAsync();

                    VacunaAplicada primeraVacunaAplicada = await _context.VacunaAplicada
                        .Where(va => va.IdDosis == primeraDosisAplicada.Id
                            && va.Dni == dni)
                        .FirstOrDefaultAsync();

                    if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Primera Dosis", descripcionVacuna))
                    {
                        if ((DateTime.Now - ultimaVacunaAplicada.FechaVacunacion).TotalDays >= 30 &&
                            (DateTime.Now - ultimaVacunaAplicada.FechaVacunacion).TotalDays < 180)
                        {
                            proximaDosis = string.Format("{0} - Segunda Dosis", descripcionVacuna);
                        }
                        else if ((DateTime.Now - ultimaVacunaAplicada.FechaVacunacion).TotalDays < 30)
                        {
                            alertasVacunacion.Add(string.Format("No se han cumplido 30 días desde la primera dosis"));
                            proximaDosis = string.Format("{0} - Segunda Dosis", descripcionVacuna);
                        }
                        else if ((DateTime.Now - ultimaVacunaAplicada.FechaVacunacion).TotalDays >= 180)
                        {
                            proximaDosis = string.Format("{0} - Tercera Dosis", descripcionVacuna);
                        }
                    }
                    else if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Segunda Dosis", descripcionVacuna))
                    {
                        if ((DateTime.Now - primeraVacunaAplicada.FechaVacunacion).TotalDays < 180)
                        {
                            alertasVacunacion.Add("La tercera dosis debe aplicarse a los 6 meses de la primera dosis");
                        }
                        proximaDosis = string.Format("{0} - Tercera Dosis", descripcionVacuna);
                    }
                    else if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Tercera Dosis", descripcionVacuna))
                    {
                        alertasVacunacion.Add("Todas las dosis fueron aplicadas");
                        proximaDosis = null;
                    }
                }

                if (embarazada)
                    alertasVacunacion.Add("La persona está embarazada");

                listaProximasDosis.Add(proximaDosis);
                listaResultado.Add(listaProximasDosis);
                listaResultado.Add(alertasVacunacion);
            }
            catch
            {

            }

            return listaResultado;
        }

        public List<List<string>> ObtenerProximaDosisBCG(DateTime fechaNacimiento, List<Dosis> dosisAplicadas, string descripcionVacuna, bool embarazada, bool personalSalud)
        {
            string proximaDosis = null;
            List<string> listaProximasDosis = new List<string>();
            List<string> alertasVacunacion = new List<string>();
            List<List<string>> listaResultado = new List<List<string>>();

            try
            {
                if (dosisAplicadas.Count == 0)
                {
                    double totalHoras = (DateTime.Now - fechaNacimiento).TotalHours;
                    proximaDosis = string.Format("{0} - Dosis Única Nacimiento", descripcionVacuna);

                    if (totalHoras > 1)
                        alertasVacunacion.Add("La dosis debe aplicarse al momento del nacimiento");
                }
                else
                {
                    alertasVacunacion.Add("Todas las dosis fueron aplicadas");
                    proximaDosis = null;
                }

                if (embarazada)
                    alertasVacunacion.Add("La persona está embarazada");

                if (personalSalud)
                    alertasVacunacion.Add("La persona es personal de salud");

                listaProximasDosis.Add(proximaDosis);
                listaResultado.Add(listaProximasDosis);
                listaResultado.Add(alertasVacunacion);
            }
            catch
            {

            }

            return listaResultado;
        }

        public List<List<string>> ObtenerProximaDosisRotavirus(DateTime fechaNacimiento, List<Dosis> dosisAplicadas, string descripcionVacuna, bool embarazada, bool personalSalud)
        {
            string proximaDosis = null;
            List<string> listaProximasDosis = new List<string>();
            List<string> alertasVacunacion = new List<string>();
            List<List<string>> listaResultado = new List<List<string>>();

            try
            {
                if (dosisAplicadas.Count == 0)
                {
                    if ((DateTime.Now - fechaNacimiento).TotalDays < 60)
                    {
                        proximaDosis = string.Format("{0} - Primera Dosis", descripcionVacuna);
                        alertasVacunacion.Add("La primera dosis debe aplicarse a partir de los 2 meses de vida");
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 60 && (DateTime.Now - fechaNacimiento).TotalDays < 104)
                    {
                        proximaDosis = string.Format("{0} - Primera Dosis", descripcionVacuna);
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 104 && (DateTime.Now - fechaNacimiento).TotalDays < 120)
                    {
                        alertasVacunacion.Add("La primera dosis debe aplicarse antes de los 104 días de vida");
                        proximaDosis = string.Format("{0} - Primera Dosis", descripcionVacuna);
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 120 && (DateTime.Now - fechaNacimiento).TotalDays < 180)
                    {
                        proximaDosis = string.Format("{0} - Segunda Dosis", descripcionVacuna);
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 180)
                    {
                        alertasVacunacion.Add("La segunda dosis debe aplicarse antes de los 6 meses de vida");
                        proximaDosis = string.Format("{0} - Segunda Dosis", descripcionVacuna);
                    }
                }
                else
                {
                    Dosis ultimaDosisAplicada = dosisAplicadas.Last();

                    if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Primera Dosis", descripcionVacuna))
                    {
                        if ((DateTime.Now - fechaNacimiento).TotalDays >= 120 && (DateTime.Now - fechaNacimiento).TotalDays < 180)
                        {
                            proximaDosis = string.Format("{0} - Segunda Dosis", descripcionVacuna);
                        }
                        else if ((DateTime.Now - fechaNacimiento).TotalDays >= 180)
                        {
                            alertasVacunacion.Add("La segunda dosis debe aplicarse antes de los 6 meses de vida");
                            proximaDosis = string.Format("{0} - Segunda Dosis", descripcionVacuna);
                        }
                    }
                    else if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Segunda Dosis", descripcionVacuna))
                    {
                        alertasVacunacion.Add("Todas las dosis fueron aplicadas");
                        proximaDosis = null;
                    }
                }

                if (embarazada)
                    alertasVacunacion.Add("La persona está embarazada");

                if (personalSalud)
                    alertasVacunacion.Add("La persona es personal de salud");

                listaProximasDosis.Add(proximaDosis);
                listaResultado.Add(listaProximasDosis);
                listaResultado.Add(alertasVacunacion);
            }
            catch
            {

            }

            return listaResultado;
        }

        public List<List<string>> ObtenerProximaDosisNeumococoConjugada(DateTime fechaNacimiento, List<Dosis> dosisAplicadas, string descripcionVacuna, bool embarazada, bool personalSalud)
        {
            string proximaDosis = null;
            List<string> listaProximasDosis = new List<string>();
            List<string> alertasVacunacion = new List<string>();
            List<List<string>> listaResultado = new List<List<string>>();

            try
            {
                if (dosisAplicadas.Count == 0)
                {
                    if ((DateTime.Now - fechaNacimiento).TotalDays < 60)
                    {
                        proximaDosis = string.Format("{0} - Primera Dosis", descripcionVacuna);
                        alertasVacunacion.Add("La primera dosis debe aplicarse a partir de los 2 meses de vida");
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 60 && (DateTime.Now - fechaNacimiento).TotalDays < 120)
                    {
                        proximaDosis = string.Format("{0} - Primera Dosis", descripcionVacuna);
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 120 && (DateTime.Now - fechaNacimiento).TotalDays < 365)
                    {
                        proximaDosis = string.Format("{0} - Segunda Dosis", descripcionVacuna);
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 365)
                    {
                        proximaDosis = string.Format("{0} - Refuerzo", descripcionVacuna);
                    }
                }
                else
                {
                    Dosis ultimaDosisAplicada = dosisAplicadas.Last();

                    if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Primera Dosis", descripcionVacuna))
                    {
                        if ((DateTime.Now - fechaNacimiento).TotalDays < 120)
                        {
                            alertasVacunacion.Add("La segunda dosis debe aplicarse a partir de los 4 meses de vida");
                            proximaDosis = string.Format("{0} - Segunda Dosis", descripcionVacuna);
                        }
                        if ((DateTime.Now - fechaNacimiento).TotalDays >= 120 && (DateTime.Now - fechaNacimiento).TotalDays < 365)
                        {
                            proximaDosis = string.Format("{0} - Segunda Dosis", descripcionVacuna);
                        }
                        if ((DateTime.Now - fechaNacimiento).TotalDays >= 365)
                        {
                            proximaDosis = string.Format("{0} - Refuerzo", descripcionVacuna);
                        }
                    }
                    else if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Segunda Dosis", descripcionVacuna))
                    {
                        proximaDosis = string.Format("{0} - Refuerzo", descripcionVacuna);

                        if ((DateTime.Now - fechaNacimiento).TotalDays < 365)
                        {
                            alertasVacunacion.Add("La dosis refuerzo debe aplicarse a partir de los 365 días de vida");
                        }
                    }
                    else if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Refuerzo", descripcionVacuna))
                    {
                        alertasVacunacion.Add("Todas las dosis fueron aplicadas");
                        proximaDosis = null;
                    }
                }

                if (embarazada)
                    alertasVacunacion.Add("La persona está embarazada");

                if (personalSalud)
                    alertasVacunacion.Add("La persona es personal de salud");

                listaProximasDosis.Add(proximaDosis);
                listaResultado.Add(listaProximasDosis);
                listaResultado.Add(alertasVacunacion);
            }
            catch
            {

            }

            return listaResultado;
        }

        public List<List<string>> ObtenerProximaDosisQuintuplePentavalente(DateTime fechaNacimiento, List<Dosis> dosisAplicadas, string descripcionVacuna, bool embarazada, bool personalSalud)
        {
            string proximaDosis = null;
            List<string> listaProximasDosis = new List<string>();
            List<string> alertasVacunacion = new List<string>();
            List<List<string>> listaResultado = new List<List<string>>();

            try
            {
                if (dosisAplicadas.Count == 0)
                {
                    if ((DateTime.Now - fechaNacimiento).TotalDays < 60)
                    {
                        proximaDosis = string.Format("{0} - Primera Dosis", descripcionVacuna);
                        alertasVacunacion.Add("La primera dosis debe aplicarse a partir de los 2 meses de vida");
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 60 && (DateTime.Now - fechaNacimiento).TotalDays < 120)
                    {
                        proximaDosis = string.Format("{0} - Primera Dosis", descripcionVacuna);
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 120 && (DateTime.Now - fechaNacimiento).TotalDays < 180)
                    {
                        proximaDosis = string.Format("{0} - Segunda Dosis", descripcionVacuna);
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 180 && (DateTime.Now - fechaNacimiento).TotalDays < 450)
                    {
                        proximaDosis = string.Format("{0} - Tercera Dosis", descripcionVacuna);
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 450 && (DateTime.Now - fechaNacimiento).TotalDays < 540)
                    {
                        proximaDosis = string.Format("{0} - Refuerzo", descripcionVacuna);
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 540)
                    {
                        proximaDosis = string.Format("{0} - Refuerzo", descripcionVacuna);
                        alertasVacunacion.Add("La dosis refuerzo debe aplicarse hasta los 18 meses de vida");
                    }
                }
                else
                {
                    Dosis ultimaDosisAplicada = dosisAplicadas.Last();

                    if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Primera Dosis", descripcionVacuna))
                    {
                        if ((DateTime.Now - fechaNacimiento).TotalDays < 120)
                        {
                            alertasVacunacion.Add("La segunda dosis debe aplicarse a partir de los 4 meses de vida");
                            proximaDosis = string.Format("{0} - Segunda Dosis", descripcionVacuna);
                        }
                        if ((DateTime.Now - fechaNacimiento).TotalDays >= 120 && (DateTime.Now - fechaNacimiento).TotalDays < 180)
                        {
                            proximaDosis = string.Format("{0} - Segunda Dosis", descripcionVacuna);
                        }
                        if ((DateTime.Now - fechaNacimiento).TotalDays >= 180 && (DateTime.Now - fechaNacimiento).TotalDays < 450)
                        {
                            proximaDosis = string.Format("{0} - Tercera Dosis", descripcionVacuna);
                        }
                        if ((DateTime.Now - fechaNacimiento).TotalDays >= 450 && (DateTime.Now - fechaNacimiento).TotalDays < 540)
                        {
                            proximaDosis = string.Format("{0} - Refuerzo", descripcionVacuna);
                        }
                        if ((DateTime.Now - fechaNacimiento).TotalDays >= 540)
                        {
                            proximaDosis = string.Format("{0} - Refuerzo", descripcionVacuna);
                            alertasVacunacion.Add("La dosis refuerzo debe aplicarse hasta los 18 meses de vida");
                        }
                    }
                    else if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Segunda Dosis", descripcionVacuna))
                    {
                        if ((DateTime.Now - fechaNacimiento).TotalDays < 180)
                        {
                            alertasVacunacion.Add("La tercera dosis debe aplicarse a partir de los 6 meses de vida");
                            proximaDosis = string.Format("{0} - Tercera Dosis", descripcionVacuna);
                        }
                        if ((DateTime.Now - fechaNacimiento).TotalDays >= 180 && (DateTime.Now - fechaNacimiento).TotalDays < 450)
                        {
                            proximaDosis = string.Format("{0} - Tercera Dosis", descripcionVacuna);
                        }
                        if ((DateTime.Now - fechaNacimiento).TotalDays >= 450 && (DateTime.Now - fechaNacimiento).TotalDays < 540)
                        {
                            proximaDosis = string.Format("{0} - Refuerzo", descripcionVacuna);
                        }
                        if ((DateTime.Now - fechaNacimiento).TotalDays >= 540)
                        {
                            proximaDosis = string.Format("{0} - Refuerzo", descripcionVacuna);
                            alertasVacunacion.Add("La dosis refuerzo debe aplicarse hasta los 18 meses de vida");
                        }
                    }
                    else if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Tercera Dosis", descripcionVacuna))
                    {
                        proximaDosis = string.Format("{0} - Refuerzo", descripcionVacuna);

                        if ((DateTime.Now - fechaNacimiento).TotalDays < 450)
                        {
                            alertasVacunacion.Add("La dosis refuerzo debe aplicarse a partir de los 15 meses de vida");
                        }
                        if ((DateTime.Now - fechaNacimiento).TotalDays >= 540)
                        {
                            alertasVacunacion.Add("La dosis refuerzo debe aplicarse hasta los 18 meses de vida");
                        }
                    }
                    else if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Refuerzo", descripcionVacuna))
                    {
                        alertasVacunacion.Add("Todas las dosis fueron aplicadas");
                        proximaDosis = null;
                    }
                }

                if (embarazada)
                    alertasVacunacion.Add("La persona está embarazada");

                if (personalSalud)
                    alertasVacunacion.Add("La persona es personal de salud");

                listaProximasDosis.Add(proximaDosis);
                listaResultado.Add(listaProximasDosis);
                listaResultado.Add(alertasVacunacion);
            }
            catch
            {

            }

            return listaResultado;
        }

        public List<List<string>> ObtenerProximaDosisSalkIPV(DateTime fechaNacimiento, List<Dosis> dosisAplicadas, string descripcionVacuna, bool embarazada, bool personalSalud)
        {
            string proximaDosis = null;
            List<string> listaProximasDosis = new List<string>();
            List<string> alertasVacunacion = new List<string>();
            List<List<string>> listaResultado = new List<List<string>>();

            try
            {
                if (dosisAplicadas.Count == 0)
                {
                    if ((DateTime.Now - fechaNacimiento).TotalDays < 60)
                    {
                        proximaDosis = string.Format("{0} - Primera Dosis", descripcionVacuna);
                        alertasVacunacion.Add("La primera dosis debe aplicarse a partir de los 2 meses de vida");
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 60 && (DateTime.Now - fechaNacimiento).TotalDays < 120)
                    {
                        proximaDosis = string.Format("{0} - Primera Dosis", descripcionVacuna);
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 120 && (DateTime.Now - fechaNacimiento).TotalDays < 180)
                    {
                        proximaDosis = string.Format("{0} - Segunda Dosis", descripcionVacuna);
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 180 && (DateTime.Now - fechaNacimiento).TotalDays < 1825)
                    {
                        proximaDosis = string.Format("{0} - Tercera Dosis", descripcionVacuna);
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 1825 && (DateTime.Now - fechaNacimiento).TotalDays < 2190)
                    {
                        proximaDosis = string.Format("{0} - Refuerzo", descripcionVacuna);
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 2190)
                    {
                        proximaDosis = string.Format("{0} - Refuerzo", descripcionVacuna);
                        alertasVacunacion.Add("La dosis refuerzo debe aplicarse hasta los 6 años de edad");
                    }
                }
                else
                {
                    Dosis ultimaDosisAplicada = dosisAplicadas.Last();

                    if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Primera Dosis", descripcionVacuna))
                    {
                        if ((DateTime.Now - fechaNacimiento).TotalDays < 120)
                        {
                            alertasVacunacion.Add("La segunda dosis debe aplicarse a partir de los 4 meses de vida");
                            proximaDosis = string.Format("{0} - Segunda Dosis", descripcionVacuna);
                        }
                        if ((DateTime.Now - fechaNacimiento).TotalDays >= 120 && (DateTime.Now - fechaNacimiento).TotalDays < 180)
                        {
                            proximaDosis = string.Format("{0} - Segunda Dosis", descripcionVacuna);
                        }
                        if ((DateTime.Now - fechaNacimiento).TotalDays >= 180 && (DateTime.Now - fechaNacimiento).TotalDays < 1825)
                        {
                            proximaDosis = string.Format("{0} - Tercera Dosis", descripcionVacuna);
                        }
                        if ((DateTime.Now - fechaNacimiento).TotalDays >= 1825 && (DateTime.Now - fechaNacimiento).TotalDays < 2190)
                        {
                            proximaDosis = string.Format("{0} - Refuerzo", descripcionVacuna);
                        }
                        if ((DateTime.Now - fechaNacimiento).TotalDays >= 2190)
                        {
                            proximaDosis = string.Format("{0} - Refuerzo", descripcionVacuna);
                            alertasVacunacion.Add("La dosis refuerzo debe aplicarse hasta los 6 años de edad");
                        }
                    }
                    else if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Segunda Dosis", descripcionVacuna))
                    {
                        if ((DateTime.Now - fechaNacimiento).TotalDays < 180)
                        {
                            alertasVacunacion.Add("La tercera dosis debe aplicarse a partir de los 6 meses de vida");
                            proximaDosis = string.Format("{0} - Tercera Dosis", descripcionVacuna);
                        }
                        if ((DateTime.Now - fechaNacimiento).TotalDays >= 180 && (DateTime.Now - fechaNacimiento).TotalDays < 1825)
                        {
                            proximaDosis = string.Format("{0} - Tercera Dosis", descripcionVacuna);
                        }
                        if ((DateTime.Now - fechaNacimiento).TotalDays >= 1825 && (DateTime.Now - fechaNacimiento).TotalDays < 2190)
                        {
                            proximaDosis = string.Format("{0} - Refuerzo", descripcionVacuna);
                        }
                        if ((DateTime.Now - fechaNacimiento).TotalDays >= 2190)
                        {
                            proximaDosis = string.Format("{0} - Refuerzo", descripcionVacuna);
                            alertasVacunacion.Add("La dosis refuerzo debe aplicarse hasta los 6 años de edad");
                        }
                    }
                    else if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Tercera Dosis", descripcionVacuna))
                    {
                        proximaDosis = string.Format("{0} - Refuerzo", descripcionVacuna);

                        if ((DateTime.Now - fechaNacimiento).TotalDays < 1825)
                        {
                            alertasVacunacion.Add("La dosis refuerzo debe aplicarse a partir de los 5 años de edad");
                        }
                        if ((DateTime.Now - fechaNacimiento).TotalDays >= 2190)
                        {
                            alertasVacunacion.Add("La dosis refuerzo debe aplicarse hasta los 6 años de edad");
                        }
                    }
                    else if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Refuerzo", descripcionVacuna))
                    {
                        alertasVacunacion.Add("Todas las dosis fueron aplicadas");
                        proximaDosis = null;
                    }
                }

                if (embarazada)
                    alertasVacunacion.Add("La persona está embarazada");

                if (personalSalud)
                    alertasVacunacion.Add("La persona es personal de salud");

                listaProximasDosis.Add(proximaDosis);
                listaResultado.Add(listaProximasDosis);
                listaResultado.Add(alertasVacunacion);
            }
            catch
            {

            }

            return listaResultado;
        }

        public List<List<string>> ObtenerProximaDosisMeningococicaConjugada(DateTime fechaNacimiento, List<Dosis> dosisAplicadas, string descripcionVacuna, bool embarazada, bool personalSalud)
        {
            string proximaDosis = null;
            List<string> listaProximasDosis = new List<string>();
            List<string> alertasVacunacion = new List<string>();
            List<List<string>> listaResultado = new List<List<string>>();

            try
            {
                if (dosisAplicadas.Count == 0)
                {
                    if ((DateTime.Now - fechaNacimiento).TotalDays < 90)
                    {
                        proximaDosis = string.Format("{0} - Primera Dosis", descripcionVacuna);
                        alertasVacunacion.Add("La primera dosis debe aplicarse a partir de los 3 meses de vida");
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 90 && (DateTime.Now - fechaNacimiento).TotalDays < 150)
                    {
                        proximaDosis = string.Format("{0} - Primera Dosis", descripcionVacuna);
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 150 && (DateTime.Now - fechaNacimiento).TotalDays < 450)
                    {
                        proximaDosis = string.Format("{0} - Segunda Dosis", descripcionVacuna);
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 450 && (DateTime.Now - fechaNacimiento).TotalDays < 4015)
                    {
                        proximaDosis = string.Format("{0} - Refuerzo", descripcionVacuna);
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 4015)
                    {
                        proximaDosis = string.Format("{0} - Dosis Única", descripcionVacuna);
                    }
                }
                else
                {
                    Dosis ultimaDosisAplicada = dosisAplicadas.Last();

                    if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Primera Dosis", descripcionVacuna))
                    {
                        if ((DateTime.Now - fechaNacimiento).TotalDays < 150)
                        {
                            alertasVacunacion.Add("La segunda dosis debe aplicarse a partir de los 5 meses de vida");
                            proximaDosis = string.Format("{0} - Segunda Dosis", descripcionVacuna);
                        }
                        if ((DateTime.Now - fechaNacimiento).TotalDays >= 150 && (DateTime.Now - fechaNacimiento).TotalDays < 450)
                        {
                            proximaDosis = string.Format("{0} - Segunda Dosis", descripcionVacuna);
                        }
                        if ((DateTime.Now - fechaNacimiento).TotalDays >= 450 && (DateTime.Now - fechaNacimiento).TotalDays < 4015)
                        {
                            proximaDosis = string.Format("{0} - Refuerzo", descripcionVacuna);
                        }
                        if ((DateTime.Now - fechaNacimiento).TotalDays >= 4015)
                        {
                            proximaDosis = string.Format("{0} - Dosis Única", descripcionVacuna);
                        }
                    }
                    else if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Segunda Dosis", descripcionVacuna))
                    {
                        if ((DateTime.Now - fechaNacimiento).TotalDays < 450)
                        {
                            alertasVacunacion.Add("La dosis refuerzo debe aplicarse a partir de los 15 meses de vida");
                            proximaDosis = string.Format("{0} - Refuerzo", descripcionVacuna);
                        }
                        if ((DateTime.Now - fechaNacimiento).TotalDays >= 450 && (DateTime.Now - fechaNacimiento).TotalDays < 4015)
                        {
                            proximaDosis = string.Format("{0} - Refuerzo", descripcionVacuna);
                        }
                        if ((DateTime.Now - fechaNacimiento).TotalDays >= 4015)
                        {
                            proximaDosis = string.Format("{0} - Dosis Única", descripcionVacuna);
                        }
                    }
                    else if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Refuerzo", descripcionVacuna))
                    {
                        proximaDosis = string.Format("{0} - Dosis Única", descripcionVacuna);

                        if ((DateTime.Now - fechaNacimiento).TotalDays < 4015)
                        {
                            alertasVacunacion.Add("La dosis única debe aplicarse a partir de los 11 años de edad");
                        }
                    }
                    else if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Dosis Única", descripcionVacuna))
                    {
                        alertasVacunacion.Add("Todas las dosis fueron aplicadas");
                        proximaDosis = null;
                    }
                }

                if (embarazada)
                    alertasVacunacion.Add("La persona está embarazada");

                if (personalSalud)
                    alertasVacunacion.Add("La persona es personal de salud");

                listaProximasDosis.Add(proximaDosis);
                listaResultado.Add(listaProximasDosis);
                listaResultado.Add(alertasVacunacion);
            }
            catch
            {

            }

            return listaResultado;
        }

        public List<List<string>> ObtenerProximaDosisTripleViralSRP(DateTime fechaNacimiento, List<Dosis> dosisAplicadas, string descripcionVacuna, bool embarazada, bool personalSalud)
        {
            string proximaDosis = null;
            List<string> listaProximasDosis = new List<string>();
            List<string> alertasVacunacion = new List<string>();
            List<List<string>> listaResultado = new List<List<string>>();

            try
            {
                if (dosisAplicadas.Count == 0)
                {
                    if ((DateTime.Now - fechaNacimiento).TotalDays < 365)
                    {
                        proximaDosis = string.Format("{0} - Primera Dosis", descripcionVacuna);
                        alertasVacunacion.Add("La primera dosis debe aplicarse a partir de los 12 meses de vida");
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 365 && (DateTime.Now - fechaNacimiento).TotalDays < 1825)
                    {
                        proximaDosis = string.Format("{0} - Primera Dosis", descripcionVacuna);
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 1825 && (DateTime.Now - fechaNacimiento).TotalDays < 2190)
                    {
                        proximaDosis = string.Format("{0} - Segunda Dosis", descripcionVacuna);
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 2190 && (DateTime.Now - fechaNacimiento).TotalDays < 4015)
                    {
                        alertasVacunacion.Add("La dosis de refuerzo debe aplicarse a partir de los 11 años. No hay aplicaciones previas, aplicar 1 dosis o bien 1 dosis de Triple Viral + 1 dosis de Doble Viral");
                        proximaDosis = string.Format("{0} - Refuerzo Triple Viral + Doble Viral", descripcionVacuna);
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 4015)
                    {
                        proximaDosis = string.Format("{0} - Refuerzo Triple Viral + Doble Viral", descripcionVacuna);
                        alertasVacunacion.Add("No hay aplicaciones previas, aplicar 1 dosis o bien 1 dosis de Triple Viral + 1 dosis de Doble Viral");
                    }
                }
                else
                {
                    Dosis ultimaDosisAplicada = dosisAplicadas.Last();

                    if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Primera Dosis", descripcionVacuna))
                    {
                        if ((DateTime.Now - fechaNacimiento).TotalDays < 1825)
                        {
                            alertasVacunacion.Add("La segunda dosis debe aplicarse a partir de los 5 años");
                            proximaDosis = string.Format("{0} - Segunda Dosis", descripcionVacuna);
                        }
                        if ((DateTime.Now - fechaNacimiento).TotalDays >= 1825 && (DateTime.Now - fechaNacimiento).TotalDays < 2190)
                        {
                            proximaDosis = string.Format("{0} - Segunda Dosis", descripcionVacuna);
                        }
                        if ((DateTime.Now - fechaNacimiento).TotalDays >= 2190 && (DateTime.Now - fechaNacimiento).TotalDays < 4015)
                        {
                            alertasVacunacion.Add("La dosis de refuerzo debe aplicarse a partir de los 11 años");
                            proximaDosis = string.Format("{0} - Refuerzo Triple Viral + Doble Viral", descripcionVacuna);
                        }
                        if ((DateTime.Now - fechaNacimiento).TotalDays >= 4015)
                        {
                            proximaDosis = string.Format("{0} - Refuerzo Triple Viral + Doble Viral", descripcionVacuna);
                        }
                    }
                    else if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Segunda Dosis", descripcionVacuna))
                    {
                        if ((DateTime.Now - fechaNacimiento).TotalDays < 4015)
                        {

                            proximaDosis = string.Format("{0} - Refuerzo Triple Viral + Doble Viral", descripcionVacuna);

                            if (dosisAplicadas.Count == 2)
                                alertasVacunacion.Add("La dosis de refuerzo debe aplicarse a partir de los 11 años. La persona recibió la primera y segunda dosis");
                            else
                                alertasVacunacion.Add("La dosis de refuerzo debe aplicarse a partir de los 11 años");
                        }
                        if ((DateTime.Now - fechaNacimiento).TotalDays >= 4015)
                        {
                            proximaDosis = string.Format("{0} - Refuerzo Triple Viral + Doble Viral", descripcionVacuna);

                            if (dosisAplicadas.Count == 2)
                                alertasVacunacion.Add("La persona recibió la primera y segunda dosis");
                        }
                    }
                    else if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Refuerzo Triple Viral + Doble Viral", descripcionVacuna))
                    {
                        alertasVacunacion.Add("Todas las dosis fueron aplicadas");
                        proximaDosis = null;
                    }
                }

                if (embarazada)
                    alertasVacunacion.Add("La persona está embarazada");

                if (personalSalud)
                    alertasVacunacion.Add("La persona es personal de salud");

                listaProximasDosis.Add(proximaDosis);
                listaResultado.Add(listaProximasDosis);
                listaResultado.Add(alertasVacunacion);
            }
            catch
            {

            }

            return listaResultado;
        }

        public List<List<string>> ObtenerProximaDosisHepatitisAHA(DateTime fechaNacimiento, List<Dosis> dosisAplicadas, string descripcionVacuna, bool embarazada, bool personalSalud)
        {
            string proximaDosis = null;
            List<string> listaProximasDosis = new List<string>();
            List<string> alertasVacunacion = new List<string>();
            List<List<string>> listaResultado = new List<List<string>>();

            try
            {
                if (dosisAplicadas.Count == 0)
                {
                    if ((DateTime.Now - fechaNacimiento).TotalDays < 365)
                    {
                        proximaDosis = string.Format("{0} - Dosis Única", descripcionVacuna);
                        alertasVacunacion.Add("La primera dosis debe aplicarse a partir de los 12 meses de vida");
                    }
                }
                else
                {
                    Dosis ultimaDosisAplicada = dosisAplicadas.Last();

                    if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Dosis Única", descripcionVacuna))
                    {
                        alertasVacunacion.Add("Todas las dosis fueron aplicadas");
                        proximaDosis = null;
                    }
                }

                if (embarazada)
                    alertasVacunacion.Add("La persona está embarazada");

                if (personalSalud)
                    alertasVacunacion.Add("La persona es personal de salud");

                listaProximasDosis.Add(proximaDosis);
                listaResultado.Add(listaProximasDosis);
                listaResultado.Add(alertasVacunacion);
            }
            catch
            {

            }

            return listaResultado;
        }

        public List<List<string>> ObtenerProximaDosisVaricela(DateTime fechaNacimiento, List<Dosis> dosisAplicadas, string descripcionVacuna, bool embarazada, bool personalSalud)
        {
            string proximaDosis = null;
            List<string> listaProximasDosis = new List<string>();
            List<string> alertasVacunacion = new List<string>();
            List<List<string>> listaResultado = new List<List<string>>();

            try
            {
                if (dosisAplicadas.Count == 0)
                {
                    if ((DateTime.Now - fechaNacimiento).TotalDays < 450)
                    {
                        proximaDosis = string.Format("{0} - Primera Dosis", descripcionVacuna);
                        alertasVacunacion.Add("La primera dosis debe aplicarse a partir de los 15 meses de vida");
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 450 && (DateTime.Now - fechaNacimiento).TotalDays < 1825)
                    {
                        proximaDosis = string.Format("{0} - Primera Dosis", descripcionVacuna);
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 1825 && (DateTime.Now - fechaNacimiento).TotalDays < 2190)
                    {
                        proximaDosis = string.Format("{0} - Segunda Dosis", descripcionVacuna);
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 2190)
                    {
                        proximaDosis = string.Format("{0} - Segunda Dosis", descripcionVacuna);
                        alertasVacunacion.Add("La segunda dosis debe aplicarse hasta los 6 años");
                    }
                }
                else
                {
                    Dosis ultimaDosisAplicada = dosisAplicadas.Last();

                    if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Primera Dosis", descripcionVacuna))
                    {
                        if ((DateTime.Now - fechaNacimiento).TotalDays < 1825)
                        {
                            alertasVacunacion.Add("La segunda dosis debe aplicarse a partir de los 5 años");
                            proximaDosis = string.Format("{0} - Segunda Dosis", descripcionVacuna);
                        }
                        if ((DateTime.Now - fechaNacimiento).TotalDays >= 1825 && (DateTime.Now - fechaNacimiento).TotalDays < 2190)
                        {
                            proximaDosis = string.Format("{0} - Segunda Dosis", descripcionVacuna);
                        }
                        if ((DateTime.Now - fechaNacimiento).TotalDays >= 2190)
                        {
                            proximaDosis = string.Format("{0} - Segunda Dosis", descripcionVacuna);
                            alertasVacunacion.Add("La segunda dosis debe aplicarse hasta los 6 años");
                        }
                    }
                    else if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Segunda Dosis", descripcionVacuna))
                    {
                        alertasVacunacion.Add("Todas las dosis fueron aplicadas");
                        proximaDosis = null;
                    }
                }

                if (embarazada)
                    alertasVacunacion.Add("La persona está embarazada");

                if (personalSalud)
                    alertasVacunacion.Add("La persona es personal de salud");

                listaProximasDosis.Add(proximaDosis);
                listaResultado.Add(listaProximasDosis);
                listaResultado.Add(alertasVacunacion);
            }
            catch
            {

            }

            return listaResultado;
        }

        public List<List<string>> ObtenerProximaDosisTripleBacterianaDTP(DateTime fechaNacimiento, List<Dosis> dosisAplicadas, string descripcionVacuna, bool embarazada, bool personalSalud)
        {
            string proximaDosis = null;
            List<string> listaProximasDosis = new List<string>();
            List<string> alertasVacunacion = new List<string>();
            List<List<string>> listaResultado = new List<List<string>>();

            try
            {
                if (dosisAplicadas.Count == 0)
                {
                    if ((DateTime.Now - fechaNacimiento).TotalDays < 1825)
                    {
                        proximaDosis = string.Format("{0} - Refuerzo", descripcionVacuna);
                        alertasVacunacion.Add("La dosis refuerzo debe aplicarse a partir de los 5 años");
                    }
                    if ((DateTime.Now - fechaNacimiento).TotalDays >= 1825 && (DateTime.Now - fechaNacimiento).TotalDays < 2190)
                    {
                        proximaDosis = string.Format("{0} - Refuerzo", descripcionVacuna);
                    }
                    if ((DateTime.Now - fechaNacimiento).TotalDays >= 2190)
                    {
                        proximaDosis = string.Format("{0} - Refuerzo", descripcionVacuna);
                        alertasVacunacion.Add("La dosis refuerzo debe aplicarse hasta los 6 años");
                    }
                }
                else
                {
                    Dosis ultimaDosisAplicada = dosisAplicadas.Last();

                    if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Refuerzo", descripcionVacuna))
                    {
                        alertasVacunacion.Add("Todas las dosis fueron aplicadas");
                        proximaDosis = null;
                    }
                }

                if (embarazada)
                    alertasVacunacion.Add("La persona está embarazada");

                if (personalSalud)
                    alertasVacunacion.Add("La persona es personal de salud");

                listaProximasDosis.Add(proximaDosis);
                listaResultado.Add(listaProximasDosis);
                listaResultado.Add(alertasVacunacion);
            }
            catch
            {

            }

            return listaResultado;
        }

        public async Task<List<List<string>>> ObtenerProximaDosisTripleBacterianaAcelular(DateTime fechaNacimiento, List<Dosis> dosisAplicadas, string descripcionVacuna, bool embarazada, bool personalSalud, int dni)
        {
            string proximaDosis = null;
            List<string> listaProximasDosis = new List<string>();
            List<string> alertasVacunacion = new List<string>();
            List<List<string>> listaResultado = new List<List<string>>();

            try
            {
                if (dosisAplicadas.Count == 0)
                {
                    if ((DateTime.Now - fechaNacimiento).TotalDays < 4015)
                    {
                        proximaDosis = string.Format("{0} - Dosis Única", descripcionVacuna);
                        alertasVacunacion.Add("La dosis única debe aplicarse a partir de los 11 años");
                    }
                    else if (embarazada)
                    {
                        proximaDosis = string.Format("{0} - Dosis Embarazo", descripcionVacuna);
                    }
                    else if (personalSalud)
                    {
                        proximaDosis = string.Format("{0} - Dosis Personal Salud", descripcionVacuna);
                    }
                }
                else
                {
                    Dosis ultimaDosisAplicada = dosisAplicadas.Last();

                    if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Dosis Única", descripcionVacuna))
                    {
                        if (embarazada)
                        {
                            proximaDosis = string.Format("{0} - Dosis Embarazo", descripcionVacuna);
                        }
                        else if (personalSalud)
                        {
                            proximaDosis = string.Format("{0} - Dosis Personal Salud", descripcionVacuna);
                        }
                    }
                    else if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Dosis Embarazo", descripcionVacuna))
                    {
                        if (embarazada)
                        {
                            proximaDosis = string.Format("{0} - Dosis Embarazo", descripcionVacuna);
                        }
                        else if (personalSalud)
                        {
                            Dosis dosisPersonalSalud = await _context.Dosis
                                .Where(d => d.Descripcion == string.Format("{0} - Dosis Personal Salud", descripcionVacuna))
                                .FirstOrDefaultAsync();

                            VacunaAplicada ultimaPS = null;

                            List<VacunaAplicada> vacunasAplicadas = await _context.VacunaAplicada
                                .Where(va => va.IdDosis == dosisPersonalSalud.Id
                                    && va.Dni == dni)
                                .OrderBy(va => va.FechaVacunacion)
                                .ToListAsync();

                            if (vacunasAplicadas.Count > 0)
                            {
                                ultimaPS = vacunasAplicadas.Last();
                                if ((DateTime.Now - ultimaPS.FechaVacunacion).TotalDays < 1825)
                                    alertasVacunacion.Add(string.Format("No ha transcurrido un mínimo de 5 años desde la última vacunación con {0}", dosisPersonalSalud.Descripcion));
                            }
                            proximaDosis = string.Format("{0} - Dosis Personal Salud", descripcionVacuna);
                        }
                    }
                    else if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Dosis Personal Salud", descripcionVacuna))
                    {
                        if (embarazada)
                        {
                            proximaDosis = string.Format("{0} - Dosis Embarazo", descripcionVacuna);
                        }
                        else if (personalSalud)
                        {
                            Dosis dosisPersonalSalud = await _context.Dosis
                                .Where(d => d.Descripcion == string.Format("{0} - Dosis Personal Salud", descripcionVacuna))
                                .FirstOrDefaultAsync();

                            VacunaAplicada ultimaPS = null;

                            List<VacunaAplicada> vacunasAplicadas = await _context.VacunaAplicada
                                .Where(va => va.IdDosis == dosisPersonalSalud.Id
                                    && va.Dni == dni)
                                .OrderBy(va => va.FechaVacunacion)
                                .ToListAsync();

                            if (vacunasAplicadas.Count > 0)
                            {
                                ultimaPS = vacunasAplicadas.Last();
                                if ((DateTime.Now - ultimaPS.FechaVacunacion).TotalDays < 1825)
                                    alertasVacunacion.Add(string.Format("No ha transcurrido un mínimo de 5 años desde la última vacunación con {0}", dosisPersonalSalud.Descripcion));
                            }
                            proximaDosis = string.Format("{0} - Dosis Personal Salud", descripcionVacuna);
                        }
                    }
                }

                listaProximasDosis.Add(proximaDosis);
                listaResultado.Add(listaProximasDosis);
                listaResultado.Add(alertasVacunacion);
            }
            catch
            {

            }

            return listaResultado;
        }

        public async Task<List<List<string>>> ObtenerProximaDosisVPH(DateTime fechaNacimiento, List<Dosis> dosisAplicadas, string descripcionVacuna, bool embarazada, bool personalSalud, int dni)
        {
            string proximaDosis = null;
            List<string> listaProximasDosis = new List<string>();
            List<string> alertasVacunacion = new List<string>();
            List<List<string>> listaResultado = new List<List<string>>();

            try
            {
                if (dosisAplicadas.Count == 0)
                {
                    if ((DateTime.Now - fechaNacimiento).TotalDays < 4015)
                    {
                        proximaDosis = string.Format("{0} - Primera Dosis", descripcionVacuna);
                        alertasVacunacion.Add("La primera dosis debe aplicarse a partir de los 11 años");
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 4015)
                    {
                        proximaDosis = string.Format("{0} - Primera Dosis", descripcionVacuna);
                    }
                }
                else
                {
                    Dosis ultimaDosisAplicada = dosisAplicadas.Last();

                    VacunaAplicada ultimaVacunaAplicada = await _context.VacunaAplicada
                        .Where(va => va.IdDosis == ultimaDosisAplicada.Id
                            && va.Dni == dni)
                        .FirstOrDefaultAsync();

                    if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Primera Dosis", descripcionVacuna))
                    {
                        if ((DateTime.Now - ultimaVacunaAplicada.FechaVacunacion).TotalDays < 180)
                        {
                            alertasVacunacion.Add("La segunda dosis debe aplicarse a los 6 meses de la primera dosis");
                            proximaDosis = string.Format("{0} - Segunda Dosis", descripcionVacuna);
                        }
                        if ((DateTime.Now - ultimaVacunaAplicada.FechaVacunacion).TotalDays >= 180)
                        {
                            proximaDosis = string.Format("{0} - Segunda Dosis", descripcionVacuna);
                        }
                    }
                    else if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Segunda Dosis", descripcionVacuna))
                    {
                        alertasVacunacion.Add("Todas las dosis fueron aplicadas");
                        proximaDosis = null;
                    }
                }

                if (embarazada)
                    alertasVacunacion.Add("La persona está embarazada");

                if (personalSalud)
                    alertasVacunacion.Add("La persona es personal de salud");

                listaProximasDosis.Add(proximaDosis);
                listaResultado.Add(listaProximasDosis);
                listaResultado.Add(alertasVacunacion);
            }
            catch
            {

            }

            return listaResultado;
        }

        public async Task<List<List<string>>> ObtenerProximaDosisDobleBacterianaDT(DateTime fechaNacimiento, List<Dosis> dosisAplicadas, string descripcionVacuna, bool embarazada, bool personalSalud, int dni)
        {
            string proximaDosis = null;
            List<string> listaProximasDosis = new List<string>();
            List<string> alertasVacunacion = new List<string>();
            List<List<string>> listaResultado = new List<List<string>>();

            try
            {
                if (dosisAplicadas.Count == 0)
                {
                    proximaDosis = string.Format("{0} - Primera Dosis", descripcionVacuna);

                    if ((DateTime.Now - fechaNacimiento).TotalDays < 6570)
                    {
                        if (embarazada)
                            alertasVacunacion.Add("La primera dosis debe aplicarse a partir de los 18 años. Persona embarazada: reemplazar una de las tres dosis por Triple Bacteriana Acelular, después de la semana 20 de gestación");
                        else
                            alertasVacunacion.Add("La primera dosis debe aplicarse a partir de los 18 años");
                    }
                    if ((DateTime.Now - fechaNacimiento).TotalDays >= 6570)
                    {
                        if (embarazada)
                            alertasVacunacion.Add("Persona embarazada: reemplazar una de las tres dosis por Triple Bacteriana Acelular, después de la semana 20 de gestación");
                    }
                }
                else
                {
                    Dosis ultimaDosisAplicada = dosisAplicadas.Last();

                    if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Primera Dosis", descripcionVacuna))
                    {
                        proximaDosis = string.Format("{0} - Segunda Dosis", descripcionVacuna);

                        if (embarazada)
                            alertasVacunacion.Add("Persona embarazada: reemplazar una de las tres dosis por Triple Bacteriana Acelular, después de la semana 20 de gestación");
                    }
                    else if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Segunda Dosis", descripcionVacuna))
                    {
                        proximaDosis = string.Format("{0} - Tercera Dosis", descripcionVacuna);

                        if (embarazada)
                            alertasVacunacion.Add("Persona embarazada: reemplazar una de las tres dosis por Triple Bacteriana Acelular, después de la semana 20 de gestación");
                    }
                    else if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Tercera Dosis", descripcionVacuna))
                    {
                        proximaDosis = string.Format("{0} - Primera Dosis", descripcionVacuna);

                        Dosis dosis = await _context.Dosis
                            .Where(d => d.Descripcion == string.Format("{0} - Tercera Dosis", descripcionVacuna))
                            .FirstOrDefaultAsync();

                        VacunaAplicada ultima = null;

                        List<VacunaAplicada> vacunasAplicadas = await _context.VacunaAplicada
                            .Where(va => va.IdDosis == dosis.Id
                                && va.Dni == dni)
                            .OrderBy(va => va.FechaVacunacion)
                            .ToListAsync();

                        if (vacunasAplicadas.Count > 0)
                        {
                            ultima = vacunasAplicadas.Last();

                            if ((DateTime.Now - ultima.FechaVacunacion).TotalDays < 3650)
                            {
                                if (embarazada)
                                    alertasVacunacion.Add("No ha transcurrido un tiempo mínimo de 10 años desde la última vacunación. Persona embarazada: reemplazar una de las tres dosis por Triple Bacteriana Acelular, después de la semana 20 de gestación");
                                else
                                    alertasVacunacion.Add("No ha transcurrido un tiempo mínimo de 10 años desde la última vacunación");
                            }
                            if ((DateTime.Now - ultima.FechaVacunacion).TotalDays >= 3650)
                            {
                                if (embarazada)
                                    alertasVacunacion.Add("Persona embarazada: reemplazar una de las tres dosis por Triple Bacteriana Acelular, después de la semana 20 de gestación");
                            }
                        }
                    }
                }

                if (personalSalud)
                    alertasVacunacion.Add("La persona es personal de salud");

                listaProximasDosis.Add(proximaDosis);
                listaResultado.Add(listaProximasDosis);
                listaResultado.Add(alertasVacunacion);
            }
            catch
            {

            }

            return listaResultado;
        }

        public List<List<string>> ObtenerProximaDosisDobleViralSR(DateTime fechaNacimiento, List<Dosis> dosisAplicadas, string descripcionVacuna, bool embarazada, bool personalSalud)
        {
            string proximaDosis = null;
            List<string> listaProximasDosis = new List<string>();
            List<string> alertasVacunacion = new List<string>();
            List<List<string>> listaResultado = new List<List<string>>();

            try
            {
                if (dosisAplicadas.Count == 0)
                {
                    proximaDosis = string.Format("{0} - Primera Dosis", descripcionVacuna);

                    if ((DateTime.Now - fechaNacimiento).TotalDays < 4015)
                    {
                        alertasVacunacion.Add("La primera dosis debe aplicarse a partir de los 11 años. No hay aplicaciones previas, aplicar 1 dosis o bien 1 dosis de Triple Viral + 1 dosis de Doble Viral");
                    }
                    else
                    {
                        alertasVacunacion.Add("No hay aplicaciones previas, aplicar 1 dosis o bien 1 dosis de Triple Viral + 1 dosis de Doble Viral");
                    }
                }
                else
                {
                    Dosis ultimaDosisAplicada = dosisAplicadas.Last();

                    if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Primera Dosis", descripcionVacuna))
                    {
                        alertasVacunacion.Add("Todas las dosis fueron aplicadas");
                        proximaDosis = null;
                    }
                }

                if (embarazada)
                    alertasVacunacion.Add("La persona está embarazada");

                listaProximasDosis.Add(proximaDosis);
                listaResultado.Add(listaProximasDosis);
                listaResultado.Add(alertasVacunacion);
            }
            catch
            {

            }

            return listaResultado;
        }

        public async Task<List<List<string>>> ObtenerProximaDosisFiebreAmarillaFA(DateTime fechaNacimiento, List<Dosis> dosisAplicadas, string descripcionVacuna, bool embarazada, bool personalSalud, int dni)
        {
            string proximaDosis = null;
            List<string> listaProximasDosis = new List<string>();
            List<string> alertasVacunacion = new List<string>();
            List<List<string>> listaResultado = new List<List<string>>();

            try
            {
                if (dosisAplicadas.Count == 0)
                {
                    if ((DateTime.Now - fechaNacimiento).TotalDays < 540)
                    {
                        proximaDosis = string.Format("{0} - Primera Dosis", descripcionVacuna);
                        alertasVacunacion.Add("La primera dosis debe aplicarse a partir de los 18 meses");
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 540 && (DateTime.Now - fechaNacimiento).TotalDays < 4015)
                    {
                        proximaDosis = string.Format("{0} - Primera Dosis", descripcionVacuna);
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 4015)
                    {
                        proximaDosis = string.Format("{0} - Refuerzo", descripcionVacuna);
                    }
                }
                else
                {
                    Dosis ultimaDosisAplicada = dosisAplicadas.Last();

                    VacunaAplicada ultimaVacunaAplicada = await _context.VacunaAplicada
                        .Where(va => va.IdDosis == ultimaDosisAplicada.Id
                            && va.Dni == dni)
                        .FirstOrDefaultAsync();

                    if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Primera Dosis", descripcionVacuna))
                    {
                        if ((DateTime.Now - ultimaVacunaAplicada.FechaVacunacion).TotalDays < 3650)
                        {
                            alertasVacunacion.Add("La dosis refuerzo debe aplicarse a los 10 años de la primera dosis");
                            proximaDosis = string.Format("{0} - Refuerzo", descripcionVacuna);
                        }
                        if ((DateTime.Now - ultimaVacunaAplicada.FechaVacunacion).TotalDays >= 3650)
                        {
                            proximaDosis = string.Format("{0} - Refuerzo", descripcionVacuna);
                        }
                    }
                    else if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Refuerzo", descripcionVacuna))
                    {
                        alertasVacunacion.Add("Todas las dosis fueron aplicadas");
                        proximaDosis = null;
                    }
                }

                if (embarazada)
                    alertasVacunacion.Add("La persona está embarazada");

                if (personalSalud)
                    alertasVacunacion.Add("La persona es personal de salud");

                listaProximasDosis.Add(proximaDosis);
                listaResultado.Add(listaProximasDosis);
                listaResultado.Add(alertasVacunacion);
            }
            catch
            {

            }

            return listaResultado;
        }

        public List<List<string>> ObtenerProximaDosisFiebreHemorragicaArgentinaFHA(DateTime fechaNacimiento, List<Dosis> dosisAplicadas, string descripcionVacuna, bool embarazada, bool personalSalud)
        {
            string proximaDosis = null;
            List<string> listaProximasDosis = new List<string>();
            List<string> alertasVacunacion = new List<string>();
            List<List<string>> listaResultado = new List<List<string>>();

            try
            {
                if (dosisAplicadas.Count == 0)
                {
                    if ((DateTime.Now - fechaNacimiento).TotalDays < 5475)
                    {
                        proximaDosis = string.Format("{0} - Primera Dosis", descripcionVacuna);
                        alertasVacunacion.Add("La primera dosis debe aplicarse a partir de los 15 años");
                    }
                    else if ((DateTime.Now - fechaNacimiento).TotalDays >= 5475)
                    {
                        proximaDosis = string.Format("{0} - Primera Dosis", descripcionVacuna);
                    }
                }
                else
                {
                    Dosis ultimaDosisAplicada = dosisAplicadas.Last();

                    if (ultimaDosisAplicada.Descripcion == string.Format("{0} - Primera Dosis", descripcionVacuna))
                    {
                        alertasVacunacion.Add("Todas las dosis fueron aplicadas");
                        proximaDosis = null;
                    }
                }

                if (embarazada)
                    alertasVacunacion.Add("La persona está embarazada");

                if (personalSalud)
                    alertasVacunacion.Add("La persona es personal de salud");

                listaProximasDosis.Add(proximaDosis);
                listaResultado.Add(listaProximasDosis);
                listaResultado.Add(alertasVacunacion);
            }
            catch
            {

            }

            return listaResultado;
        }

        private async Task<List<string>> VerificarCredencialesUsuarioOperadorNacionalVacunador(string emailOperadorNacional, List<string> errores)
        {
            try
            {
                Usuario usuarioSolicitante = await _context.Usuario.Where(u => u.Email == emailOperadorNacional).FirstOrDefaultAsync();
                if (usuarioSolicitante == null)
                    errores.Add(string.Format("El usuario {0} no está registrado en el sistema", emailOperadorNacional));
                else
                {
                    Rol rol = await _context.Rol
                        .Where(r => r.Id == usuarioSolicitante.IdRol
                            && (r.Descripcion == "Operador Nacional" || r.Descripcion == "Vacunador"))
                        .FirstOrDefaultAsync();
                    if (rol == null)
                        errores.Add(string.Format("El usuario {0} no tiene rol operador nacional o rol vacunador", emailOperadorNacional));
                }
            }
            catch
            {

            }

            return errores;
        }
    }
}