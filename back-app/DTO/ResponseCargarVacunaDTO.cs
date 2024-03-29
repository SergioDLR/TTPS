﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VacunacionApi.DTO
{
    public class ResponseCargarVacunaDTO : ResponseCabeceraDTO
    {
        public ResponseCargarVacunaDTO(string estadoTransaccion, bool existenciaErrores, List<string> errores, string email)
        {
            EstadoTransaccion = estadoTransaccion;
            ExistenciaErrores = existenciaErrores;
            Errores = errores;
            Email = email;
        }

        public string Email { get; set; }
    }
}
