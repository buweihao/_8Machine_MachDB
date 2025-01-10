using _8Machine_MachDB.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _8Machine_MachDB.Services
{
    public class MachDBServices
    {
        public void GetLeagleData(out string[] strings, MachDBModel machDBModel)
        {
            strings = ["10001", "10002", "10003", "10004", "10005", "10006"];
        }

        public bool IsOKData(string[] DataArray, MachDBModel machDBModel)
        {
            return false;
        }



    }
}
