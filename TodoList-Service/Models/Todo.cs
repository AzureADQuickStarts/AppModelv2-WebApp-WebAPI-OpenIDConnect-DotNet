﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TodoList_Service.Models
{
    public class Todo
    {
        public int ID { get; set; }
        public string Description { get; set; }
        public string Owner { get; set; }
    }
}
