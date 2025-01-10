using _8Machine_MachDB.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _8Machine_MachDB.Interfaces
{
    public interface IMachDBServices
    {
        /// <summary>
        /// 向数据库请求一条合法数据
        /// </summary>
        /// <param name="strings">合法数据的字符串数组</param>
        /// <param name="machDBModel"></param>
        public void GetLeagleData(out string[] strings,MachDBModel machDBModel);

        /// <summary>
        /// 向数据库查询此数据是否NG
        /// </summary>
        /// <param name="DataArray"></param>
        /// <param name="machDBModel"></param>
        /// <returns></returns>
        public bool IsOKData(string[] DataArray, MachDBModel machDBModel);



    }
}
