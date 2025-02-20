using _8Machine_MachDB.Interfaces;
using _8Machine_MachDB.Models;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace _8Machine_MachDB.Services
{
    public class MachDBServices : IMachDBServices
    {
        public void GetLeagleData(out string[] strings, MachDBModel machDBModel, int delaytime)
        {
            strings = new string[0];  // 默认返回空数组

            if (delaytime > 0)
            {
                // 模拟返回一些数据（可以根据实际业务需求修改）
                strings = new string[] { "10001", "10002", "10003", "10004", "10005", "10006" };
            }
            else
            {
                try
                {
                    using (var connection = new MySqlConnection(machDBModel.connectionString))
                    {
                        connection.Open(); // 确保连接被打开

                        // 开始事务
                        using (var transaction = connection.BeginTransaction())
                        {
                            try
                            {
                                // 执行查询
                                using (var cmd = new MySqlCommand(machDBModel.GetLeagleDataString, connection, transaction))
                                {
                                    using (var reader = cmd.ExecuteReader()) // 执行查询并获取结果
                                    {
                                        if (reader.Read()) // 读取第一行数据
                                        {
                                            // 将查询到的数据存入 strings 数组
                                            var result = new List<string>();
                                            for (int i = 0; i < machDBModel.ColumnCount; i++)
                                            {
                                                result.Add(reader.IsDBNull(i) ? "NULL" : reader.GetString(i));
                                            }

                                            // 返回查询结果
                                            strings = result.ToArray();
                                        }
                                        else
                                        {
                                            Console.WriteLine("没有符合条件的数据。");
                                        }
                                    }
                                }

                                // 关闭 reader 后执行更新
                                if (strings.Length > 0)
                                {
                                    // 更新 Status 为 "已下发未使用"
                                    string updateQuery = $"UPDATE my_table_new SET Status = '已下发未使用' WHERE {machDBModel.PrimaryKeyColName} = '{strings[machDBModel.PrimaryKeyColumn]}';";

                                    // 执行更新操作
                                    using (var updateCmd = new MySqlCommand(updateQuery, connection, transaction))
                                    {
                                        updateCmd.ExecuteNonQuery();
                                    }

                                    // 提交事务
                                    transaction.Commit();
                                    Console.WriteLine("数据查询与更新成功。");
                                }
                            }
                            catch (Exception ex)
                            {
                                // 如果有异常，回滚事务
                                Console.WriteLine("执行SQL查询时发生错误: " + ex.Message);
                                transaction.Rollback();
                                strings = new string[] { "Error: " + ex.Message }; // 在出错时返回错误信息
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // 捕获连接错误或事务错误
                    Console.WriteLine("数据库连接或事务错误: " + ex.Message);
                    strings = new string[] { "Error: " + ex.Message };
                }
            }
        }

        public bool IsOKData(string DataArray, MachDBModel machDBModel, int DelayTime, string kkk)
        {
            machDBModel.times++;

            if (DelayTime != 0)
            {
                Thread.Sleep(DelayTime);
                if (machDBModel.times % 4 == 0) // 每四次调用返回false
                {
                    return false;
                }
                return true;
            }
            else
            {
                // 创建连接对象
                using (var connection = new MySqlConnection(machDBModel.connectionString))
                {
                    try
                    {
                        // 打印连接字符串，用于调试
                        Console.WriteLine($"Connection String: {machDBModel.connectionString}");

                        // 打开连接前检查连接状态
                        if (connection.State != System.Data.ConnectionState.Open)
                        {
                            Console.WriteLine("Opening connection...");
                            connection.Open();
                        }

                        // 确认连接是否成功打开
                        if (connection.State != System.Data.ConnectionState.Open)
                        {
                            Console.WriteLine("Connection failed to open.");
                            return false;
                        }

                        Console.WriteLine("Connection successfully opened.");

                        // 分割 DataArray 以获取存储过程所需的参数
                        string[] parameters = DataArray.Split(',');

                        // 打印解析的 parameters 数组
                        Console.WriteLine("Parsed parameters: ");
                        foreach (var param in parameters)
                        {
                            Console.WriteLine(param);
                        }

                        string NG_Status = parameters[parameters.Length - 1]; // NG_Status 是最后一个参数
                        int KeyColumn = machDBModel.PrimaryKeyColumn;

                        // 构建参数列表
                        var command = new MySqlCommand();
                        string storedProcedureName = $"Detect_Duplicate_{machDBModel.ColumnCount}"; // 动态选择存储过程名称

                        // 打印存储过程名称
                        Console.WriteLine($"Selected stored procedure: {storedProcedureName}");

                        // 检查 ColumnCount 是否在允许的范围内
                        if (machDBModel.ColumnCount < 1 || machDBModel.ColumnCount > 6)
                        {
                            throw new ArgumentException("Unsupported ColumnCount value. Must be between 1 and 6.");
                        }

                        // 根据 ColumnCount 绑定相应的参数
                        for (int i = 0; i < machDBModel.ColumnCount; i++)
                        {
                            string parameterName = $"p_C{i + 1}";
                            string parameterValue = parameters[i];

                            // 动态绑定 C1, C2, ..., Cn 参数
                            command.Parameters.AddWithValue(parameterName, parameterValue);

                            // 打印每个绑定的参数
                            Console.WriteLine($"Binding parameter: {parameterName} = {parameterValue}");
                        }

                        // 设置存储过程名称
                        command.CommandText = storedProcedureName;
                        command.CommandType = System.Data.CommandType.StoredProcedure;

                        // 添加其他公共参数
                        command.Parameters.AddWithValue("KeyColumn", KeyColumn);
                        command.Parameters.AddWithValue("p_NG_Status", NG_Status);

                        // 打印所有参数
                        Console.WriteLine("Parameters after binding:");
                        foreach (MySqlParameter param in command.Parameters)
                        {
                            Console.WriteLine($"{param.ParameterName} = {param.Value}");
                        }

                        // 添加输出参数
                        MySqlParameter insertSuccessParam = new MySqlParameter("p_InsertSuccess", MySqlDbType.Int32);
                        insertSuccessParam.Direction = System.Data.ParameterDirection.Output;
                        command.Parameters.Add(insertSuccessParam);

                        // 执行存储过程
                        command.ExecuteNonQuery();

                        // 获取输出参数的值
                        int insertSuccess = Convert.ToInt32(insertSuccessParam.Value);

                        // 根据输出参数的值判断是否成功插入到 my_table_old
                        return insertSuccess == 1;
                    }
                    catch (Exception ex)
                    {
                        // 打印详细错误信息
                        Console.WriteLine($"Error: {ex.Message}");
                        Console.WriteLine($"StackTrace: {ex.StackTrace}");
                        return false;
                    }
                }
            }
        }

        public bool IsOKData(string DataArray, MachDBModel machDBModel, int DelayTime,int kkk)
        {
            machDBModel.times++;

            if (DelayTime != 0)
            {
                Thread.Sleep(DelayTime);
                if (machDBModel.times % 4 == 0) // 每四次调用返回false
                {
                    return false;
                }
                return true;
            }
            else
            {
                using (var connection = new MySqlConnection(machDBModel.connectionString))
                {
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        connection.Open();
                    }

                    try
                    {
                        // 分割 DataArray 以获取存储过程所需的三个参数
                        string[] parameters = DataArray.Split(',');
                        string C1 = parameters[0];
                        string C2 = parameters[1];
                        string C3 = parameters[2];
                        string NG_Status = parameters[3];
                        int KeyColumn = machDBModel.PrimaryKeyColumn;

                        // 创建 MySqlCommand 来调用存储过程 Detect_Duplicate_3
                        using (MySqlCommand command = new MySqlCommand("Detect_Duplicate_3", connection))
                        {
                            command.CommandType = System.Data.CommandType.StoredProcedure;

                            // 添加存储过程的输入参数
                            command.Parameters.AddWithValue("p_C1", C1);
                            command.Parameters.AddWithValue("p_C2", C2);
                            command.Parameters.AddWithValue("p_C3", C3);
                            command.Parameters.AddWithValue("KeyColumn", KeyColumn);
                            command.Parameters.AddWithValue("p_NG_Status", NG_Status);

                            // 添加输出参数
                            MySqlParameter insertSuccessParam = new MySqlParameter("p_InsertSuccess", MySqlDbType.Int32);
                            insertSuccessParam.Direction = System.Data.ParameterDirection.Output;
                            command.Parameters.Add(insertSuccessParam);

                            // 执行存储过程
                            command.ExecuteNonQuery();

                            // 获取输出参数的值
                            int insertSuccess = Convert.ToInt32(insertSuccessParam.Value);

                            // 根据输出参数的值判断是否成功插入到 my_table_old
                            return insertSuccess == 1;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        return false;
                    }
                }
            }
        }
        public bool IsOKData(string DataArray, MachDBModel machDBModel, int DelayTime)
        {
            machDBModel.times++;

            if (DelayTime != 0)
            {
                Thread.Sleep(DelayTime);
                if (machDBModel.times % 4 == 0) // 每四次调用返回false
                {
                    return false;
                }
                return true;
            }
            else
            {
                using (var connection = new MySqlConnection(machDBModel.connectionString))
                {
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        connection.Open();
                    }

                    try
                    {
                        // 分割 DataArray 以获取存储过程所需的三个参数
                        string[] parameters = DataArray.Split(',');
                        string NG_Status = parameters[parameters.Length-1];
                        int KeyColumn = machDBModel.PrimaryKeyColumn;

                        // 创建 MySqlCommand 来调用存储过程 Detect_Duplicate_3
                        using (MySqlCommand command = new MySqlCommand($"Detect_Duplicate_{machDBModel.ColumnCount}", connection))
                        {
                            command.CommandType = System.Data.CommandType.StoredProcedure;

                            // 根据 ColumnCount 绑定相应的参数
                            for (int i = 0; i < machDBModel.ColumnCount; i++)
                            {
                                string parameterName = $"p_C{i + 1}";
                                string parameterValue = parameters[i];

                                // 动态绑定 C1, C2, ..., Cn 参数
                                command.Parameters.AddWithValue(parameterName, parameterValue);

                            }
                            command.Parameters.AddWithValue("KeyColumn", KeyColumn);
                            command.Parameters.AddWithValue("p_NG_Status", NG_Status);

                            // 添加输出参数
                            MySqlParameter insertSuccessParam = new MySqlParameter("p_InsertSuccess", MySqlDbType.Int32);
                            insertSuccessParam.Direction = System.Data.ParameterDirection.Output;
                            command.Parameters.Add(insertSuccessParam);

                            // 执行存储过程
                            command.ExecuteNonQuery();

                            // 获取输出参数的值
                            int insertSuccess = Convert.ToInt32(insertSuccessParam.Value);

                            // 根据输出参数的值判断是否成功插入到 my_table_old
                            return insertSuccess == 1;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error: {ex.Message}");
                        return false;
                    }
                }
            }
        }


        public bool InitMySQL(MachDBModel machDBModel)
        {
            try
            {
                // 使用using自动管理连接对象
                using (MySqlConnection connection = new MySqlConnection(machDBModel.connectionString))
                {
                    connection.Open();
                    try
                    {
                        // 创建表1
                        using (var cmd = new MySqlCommand(machDBModel.CreateOldBlankTableString, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                        // 创建表2
                        using (var cmd = new MySqlCommand(machDBModel.CreateNewBlankTableString, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }
                        // 创建表3
                        using (var cmd = new MySqlCommand(machDBModel.CreateNGBlankTableString, connection))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        // 创建存储过程
                        // using (var cmd = new MySqlCommand(createProcedureQuery, connection))
                        // {
                        //     cmd.ExecuteNonQuery();
                        // }

                        return true; // 成功
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("数据库操作失败: " + ex.Message);
                        return false; // 操作失败
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("数据库连接失败: " + ex.Message);
                return false; // 连接失败
            }
        }


        public void ClearNewBlank(MachDBModel machDBModel)
        {
            try
            {
                // 使用新的 MySQL 连接
                using (var connection = new MySqlConnection(machDBModel.connectionString))
                {
                    // 打开连接
                    connection.Open();

                    // 检查连接是否成功打开
                    if (connection.State != System.Data.ConnectionState.Open)
                    {
                        throw new InvalidOperationException("数据库连接未成功打开");
                    }

                    // 使用新的连接执行 TRUNCATE TABLE 命令
                    using (var cmd = new MySqlCommand(machDBModel.ClearNewBlankString, connection))
                    {
                        cmd.ExecuteNonQuery(); // 执行 TRUNCATE 操作
                        Console.WriteLine("TRUNCATE 操作成功。");
                    }
                }
            }
            catch (MySqlException mysqlEx)
            {
                // 专门捕获 MySQL 相关的异常
                Console.WriteLine("执行 TRUNCATE 操作时发生 MySQL 错误: " + mysqlEx.Message);
            }
            catch (Exception ex)
            {
                // 捕获其他类型的异常
                Console.WriteLine("执行 TRUNCATE 操作时发生错误: " + ex.Message);
            }
        }



        public void LoadNewBag(string path, MachDBModel machDBModel)
        {
            try
            {
                // 使用新的 MySQL 连接
                using (var connection = new MySqlConnection(machDBModel.connectionString))
                {
                    // 打开连接
                    connection.Open();

                    // 读取文件的第一行，确定列数
                    string firstLine;
                    using (var reader = new StreamReader(path))
                    {
                        firstLine = reader.ReadLine();
                    }

                    int columnCount = firstLine.Split(',').Length;

                    // 如果列数超过 6，则截断为 6 列
                    if (columnCount > 6)
                    {
                        columnCount = 6;
                        Console.WriteLine("文件列数超过 6 列，仅导入前 6 列。");
                    }

                    // 将列数存储到 MachDBModel 中
                    if (machDBModel.ColumnCount == 1413)
                    {
                        // 首次设置
                        machDBModel.ColumnCount = columnCount;
                    }
                    else
                    {
                        // 非首次设置，检查是否与之前导入的文件列数一致
                        if (machDBModel.ColumnCount != columnCount)
                        {
                            // 抛出异常，提示列数不一致
                            throw new InvalidOperationException("文件列数与之前导入的文件列数不一致，导入失败。");
                        }
                    }

                    // 动态生成列名 C1 到 C6（只包含实际的列名）
                    var columns = new List<string>();
                    for (int i = 1; i <= columnCount; i++)
                    {
                        columns.Add($"C{i}");
                    }

                    // 将列名组合成 SQL 格式
                    string columnsSql = string.Join(", ", columns);

                    // 构建 LOAD DATA INFILE 语句
                    string loadDataQuery = $@"
    LOAD DATA INFILE '{path.Replace("\\", "/")}'  -- 确保路径格式正确
    IGNORE INTO TABLE my_table_new
    FIELDS TERMINATED BY ',' 
    LINES TERMINATED BY '\r\n'
    ({columnsSql});
    ";

                    // 打印生成的 SQL 语句，调试使用
                    Console.WriteLine(loadDataQuery);

                    // 执行导入数据的 SQL 语句
                    using (var loadDataCmd = new MySqlCommand(loadDataQuery, connection))
                    {
                        loadDataCmd.ExecuteNonQuery();
                    }

                    Console.WriteLine("数据导入成功。");
                }
            }
            catch (FileNotFoundException fileEx)
            {
                Console.WriteLine($"文件未找到: {fileEx.Message}");
            }
            catch (UnauthorizedAccessException authEx)
            {
                Console.WriteLine($"没有访问权限: {authEx.Message}");
            }
            catch (MySqlException sqlEx)
            {
                Console.WriteLine($"数据库操作错误: {sqlEx.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"执行SQL查询时发生错误: {ex.Message}");
            }
        }





        public void NewBlankInpect(MachDBModel machDBModel)
        {
            try
            {
                using (var connection = new MySqlConnection(machDBModel.connectionString))
                {
                    // 显式打开连接
                    connection.Open();

                    // 创建SqlCommand对象
                    using (var command = new MySqlCommand(machDBModel.NewBlankInpectString, connection))
                    {
                        // 打印 SQL 语句，调试时使用
                        Console.WriteLine("Executing SQL: " + command.CommandText);

                        // 执行DELETE操作并获取受影响的行数
                        int rowsAffected = command.ExecuteNonQuery();
                        Console.WriteLine($"{rowsAffected} rows deleted.");
                    }
                }
            }
            catch (MySqlException sqlEx)
            {
                // MySQL 特定的异常处理
                Console.WriteLine($"MySQL Error: {sqlEx.Message}");
            }
            catch (Exception ex)
            {
                // 一般错误处理
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

        public void SetPoolSize(MachDBModel machDBModel)
        {
            try
            {
                // 使用新的 MySQL 连接
                using (var connection = new MySqlConnection(machDBModel.connectionString))
                {
                    // 显式打开连接
                    connection.Open();

                    // 创建命令并执行
                    using (var command = new MySqlCommand(machDBModel.SetPoolSizeString, connection))
                    {
                        command.ExecuteNonQuery();  // 执行查询
                    }
                }
            }
            catch (MySqlException sqlEx)
            {
                // MySQL 特定的异常处理
                Console.WriteLine($"MySQL Error: {sqlEx.Message}");
            }
            catch (Exception ex)
            {
                // 一般错误处理
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }

    }
}
