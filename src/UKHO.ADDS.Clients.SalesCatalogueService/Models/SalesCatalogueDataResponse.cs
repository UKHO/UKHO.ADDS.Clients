﻿using System.Net;

namespace UKHO.ADDS.Clients.SalesCatalogueService.Models
{
    public class SalesCatalogueDataResponse
    {
        public List<SalesCatalogueDataProductResponse> ResponseBody { get; set; }
        public HttpStatusCode ResponseCode { get; set; }
        public DateTime? LastModified { get; set; }
        public object Clone()
        {
            return this.MemberwiseClone();
        }
    }

}
