﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace VacunacionApi.DTO
{
    public class VacunaDesarrolladaDTO
    {
        public VacunaDesarrolladaDTO() { }

        public VacunaDesarrolladaDTO(int id, int idVacuna, int idMarcaComercial, int diasDeDemora, double precioVacunaDesarrollada)
        {
            Id = id;
            IdVacuna = idVacuna;
            IdMarcaComercial = idMarcaComercial;
            DiasDemoraEntrega = diasDeDemora;
            PrecioVacuna = precioVacunaDesarrollada;
            FechaDesde = DateTime.Now;
            FechaHasta = null;
        }

        public int Id { get; set; }
        public int IdVacuna { get; set; }
        public int IdMarcaComercial { get; set; }
        public int DiasDemoraEntrega { get; set; }
        public double PrecioVacuna { get; set; }
        public DateTime FechaDesde { get; set; }
        public DateTime? FechaHasta { get; set; }
    }
}