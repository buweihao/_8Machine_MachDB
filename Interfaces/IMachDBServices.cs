using _8Machine_MachDB.Models;
using MySql.Data.MySqlClient;
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
        public void GetLeagleData(out string[] strings, MachDBModel machDBModel, int delaytime);

        /// <summary>
        /// 向数据库查询此数据是否NG（重码检测）
        /// </summary>
        /// <param name="DataArray">包含明暗码信息和推理机能得出的NG情况的字符串，以逗号隔开，具体内容在InspectionAlgorithm中形成</param>
        /// <param name="machDBModel"></param>
        /// <returns></returns>
        public bool IsOKData(string DataArray, MachDBModel machDBModel, int DelayTime);

        /// <summary>
        /// 连接到MySQL数据库,并且创建必要的三个表
        /// </summary>
        /// <param name="server_address"></param>
        /// <param name="DatabaseName"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public bool InitMySQL(MachDBModel machDBModel);

        /// <summary>
        /// 加载一个新的数据包
        /// </summary>
        /// <param name="path"></param>
        /// <param name="machDBModel"></param>
        public void LoadNewBag(string path, MachDBModel machDBModel);

        /// <summary>
        /// 清空新表中的数据
        /// </summary>
        /// <param name="machDBModel"></param>
        public void ClearNewBlank(MachDBModel machDBModel);

        /// <summary>
        /// 对新码包进行历史查重
        /// </summary>
        /// <param name="machDBModel"></param>
        public void NewBlankInpect(MachDBModel machDBModel);

        /// <summary>
        /// 设置缓冲区大小
        /// </summary>
        /// <param name="size"></param>
        /// <param name="machDBModel"></param>
        public void SetPoolSize( MachDBModel machDBModel);


    }
}
