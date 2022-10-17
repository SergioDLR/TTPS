﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VacunacionApi.DTO
{
    public class JurisdiccionDTO
    {
        public JurisdiccionDTO(int id, string descripcion)
        {
            Id = id;
            Descripcion = descripcion;
        }

        public int Id { get; set; }
        public string Descripcion { get; set; }
    }
}
