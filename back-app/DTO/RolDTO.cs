﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace VacunacionApi.DTO
{
    public class RolDTO
    {
        public RolDTO(int id, string descripcion)
        {
            Id = id;
            Descripcion = descripcion;
        }

        public int Id { get; set; }
        public string Descripcion { get; set; }
    }
}
