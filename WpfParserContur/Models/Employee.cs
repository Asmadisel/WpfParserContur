using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfParserContur.Models
{
    public class Employee
    {
        public string Name { get; set; }
        public string Surname { get; set; }
        public List<Salary> Salaries { get; set; } = new List<Salary>();
        public decimal Total { get; set; }
    }
}
