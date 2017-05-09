using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SolutionHistory.Models
{
    public class HistoryItem
    {
        public string User { get; set; }
        public int Changeset { get; set; }
        public string Comment { get; set; }
        public DateTime Date { get; internal set; }
        public string Project { get; set; }
    }
}
