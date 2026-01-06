using System;

namespace V16App.API
{
    /// <summary>
    /// Data model for V16 beacon information
    /// </summary>
    [Serializable]
    public class BeaconData
    {
        public string Id;
        public string SituationId;
        public string Estado;
        public double Latitud;
        public double Longitud;
        public string Carretera;
        public float PuntoKm;
        public string Sentido;
        public string Orientacion;
        public string ComunidadAutonoma;
        public string Provincia;
        public string Municipio;
        public string Causa;
        public string SubCausa;
        public string TipoVialidad;
        public string FechaInicio;
        
        public bool IsActive => Estado?.ToLower() == "active";
        
        /// <summary>
        /// Calculate time since beacon activation
        /// </summary>
        public string GetTimeSinceActivation()
        {
            if (string.IsNullOrEmpty(FechaInicio)) return "Desconocido";
            
            try
            {
                DateTime startTime = DateTime.Parse(FechaInicio);
                TimeSpan elapsed = DateTime.Now - startTime;
                
                if (elapsed.TotalMinutes < 60)
                    return $"hace {(int)elapsed.TotalMinutes} min";
                else if (elapsed.TotalHours < 24)
                    return $"hace {(int)elapsed.TotalHours} h";
                else if (elapsed.TotalDays < 30)
                    return $"hace {(int)elapsed.TotalDays} días";
                else
                    return $"hace {(int)(elapsed.TotalDays / 30)} meses";
            }
            catch
            {
                return "Desconocido";
            }
        }
        
        /// <summary>
        /// Get Google Maps URL for this beacon location
        /// </summary>
        public string GetGoogleMapsUrl()
        {
            return $"https://www.google.com/maps?q={Latitud},{Longitud}";
        }
        
        /// <summary>
        /// Get Waze navigation URL
        /// </summary>
        public string GetWazeUrl()
        {
            return $"https://waze.com/ul?ll={Latitud},{Longitud}&navigate=yes";
        }
        
        /// <summary>
        /// Get formatted beacon display name
        /// </summary>
        public string GetDisplayName()
        {
            return $"Baliza {Id}";
        }
        
        /// <summary>
        /// Get formatted location string
        /// </summary>
        public string GetLocationString()
        {
            return $"{Municipio}, {Provincia}";
        }
    }
}
