using System;
using System.Collections.Generic;

namespace QuickApp
{
    public class ResponseMessageDto
    {
        public long Id { get; set; }
        public string SuccessMessage { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public bool Error { get; set; }
        public Object Result { get; set; }
        public List<Object> Array { get; set; }
    }
}
