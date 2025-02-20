using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _8Machine_MachDB.Models
{
    public class MachDBModel
    {
        public ulong times;

        public int ColumnCount;

        public int PrimaryKeyColumn;
        public string PrimaryKeyColName;
        public string connectionString;
        public string CreateNewBlankTableString;
        public string CreateOldBlankTableString;
        public string CreateNGBlankTableString;
        public string CreateIsNGString;
        public string ClearNewBlankString;
        public string LoadNewBagString;
        public string NewBlankInpectString;
        public string SetPoolSizeString;
        public string GetLeagleDataString;
        public string UpdateStatusString;


        public MachDBModel(string DataBaseName, int PrimaryKeyColumn,int ColumnCount, string DataBaseUserName, string DataBasePassword,int PoolSize)
        {
            this.ColumnCount = ColumnCount;
            this.PrimaryKeyColumn = PrimaryKeyColumn;
            PrimaryKeyColName = $"C{PrimaryKeyColumn}"; // 将数字转换为列名

            connectionString = $"Server=localhost;Database={DataBaseName};User Id={DataBaseUserName};Password={DataBasePassword};";

            CreateNewBlankTableString = $@"

        CREATE TABLE IF NOT EXISTS my_table_new (
            C1 VARCHAR(64),
            C2 VARCHAR(64),
            C3 VARCHAR(64),
            C4 VARCHAR(64),
            C5 VARCHAR(64),
            C6 VARCHAR(64),
            Status ENUM('未下发', '已下发未使用', '已使用') NOT NULL DEFAULT '未下发',
            PRIMARY KEY ({PrimaryKeyColName}),  -- 动态设置主键
            INDEX idx_status (Status)  -- 为 Status 字段创建索引
);";
            Console.WriteLine(CreateNewBlankTableString);
            CreateOldBlankTableString = $@"
    -- 创建新表
    CREATE TABLE IF NOT EXISTS my_table_old (
        C1 VARCHAR(64),
        C2 VARCHAR(64),
        C3 VARCHAR(64),
        C4 VARCHAR(64),
        C5 VARCHAR(64),
        C6 VARCHAR(64),
        PRIMARY KEY ({PrimaryKeyColName})  -- 动态设置主键
);";


            CreateNGBlankTableString = @"
                                           CREATE TABLE IF NOT EXISTS my_table_NG (
            C1 VARCHAR(64),
            C2 VARCHAR(64),
            C3 VARCHAR(64),
            C4 VARCHAR(64),
            C5 VARCHAR(64),
            C6 VARCHAR(64),
                                           Status ENUM('位置超差',  '角度超差', '图元缺失', '图元重复', '码已使用', '码不存在', '关联性错误') NOT NULL,
                                           INDEX (Status) -- 这里将 Status 字段设置为普通的索引，可以重复
);";

            //存储过程
            switch (ColumnCount)
            {
                case 1:
                    CreateIsNGString = @"";
                    break;
                case 2:
                    CreateIsNGString = @"";
                    break;
                case 3:
                    CreateIsNGString = @"
DROP PROCEDURE IF EXISTS Detect_Duplicate_3;
CREATE PROCEDURE Detect_Duplicate_3(
    IN p_C1 VARCHAR(255),   -- 第一列
    IN p_C2 VARCHAR(255),   -- 第二列
    IN p_C3 VARCHAR(255),   -- 第三列
    IN p_NG_Status ENUM('位置超差', '角度超差', '图元缺失', '图元重复', 'OK'),  -- NG状态
    IN KeyColumn INT,    -- 外部传入的主键列 (1= p_C1, 2= p_C2, 3= p_C3)
    OUT p_InsertSuccess INT   -- 输出参数，指示插入成功与否
)
BEGIN
    -- 声明变量
    DECLARE v_Status ENUM('未下发', '下发未检出', '已检出');
    DECLARE v_Count INT DEFAULT 0;
    DECLARE v_ExistingStatus ENUM('已使用', '已下发未使用', '未下发');  -- 存储从 my_table_new 中获取的 Status
    DECLARE v_KeyColumnValue VARCHAR(255); -- 用于存储传入的主键值
    DECLARE v_StatusNotExist ENUM('位置超差', '角度超差', '图元缺失', '图元重复', '码已使用', '码不存在', '关联性错误');
    DECLARE v_NULL VARCHAR(255) DEFAULT NULL;

    -- 初始化输出参数
    SET p_InsertSuccess = 0;

    -- 根据 KeyColumn 判断使用哪个列作为主键查找 (标号1)
    IF KeyColumn = 1 THEN
        SET v_KeyColumnValue = p_C1;
    ELSEIF KeyColumn = 2 THEN
        SET v_KeyColumnValue = p_C2;
    ELSEIF KeyColumn = 3 THEN
        SET v_KeyColumnValue = p_C3;
    END IF;

    -- 1. 查找 KeyColumn 在 my_table_new 中是否存在 (标号2)
    -- 根据 KeyColumn 判断查询的列
    IF KeyColumn = 1 THEN
        SELECT Status INTO v_ExistingStatus
        FROM my_table_new
        WHERE C1 = v_KeyColumnValue;
    ELSEIF KeyColumn = 2 THEN
        SELECT Status INTO v_ExistingStatus
        FROM my_table_new
        WHERE C2 = v_KeyColumnValue;
    ELSEIF KeyColumn = 3 THEN
        SELECT Status INTO v_ExistingStatus
        FROM my_table_new
        WHERE C3 = v_KeyColumnValue;
    END IF;

    -- 2. 如果没有找到数据，执行插入操作 (标号3)
    IF v_ExistingStatus IS NULL THEN
        -- 插入数据到 my_table_NG，Status 设置为 ""码不存在""
        SET v_StatusNotExist = '码不存在';
        INSERT INTO my_table_NG (C1, C2, C3, Status)
        VALUES (p_C1, p_C2, p_C3, v_StatusNotExist);
        
    -- 3. 如果找到了数据，检查 NG 状态 (标号4)
    ELSE
        -- 3.1 如果 NG 状态不是 'OK'，直接插入到 my_table_NG (标号5)
        IF p_NG_Status <> 'OK' THEN
            INSERT INTO my_table_NG (C1, C2, C3, Status)
            VALUES (p_C1, p_C2, p_C3, p_NG_Status);

        -- 3.2 如果 NG 状态是 'OK'，继续检查 Status 字段 (标号6)
        ELSE
            -- 3.2.1 如果 Status 为 '已使用'，插入 '码已使用' 到 my_table_NG (标号7)
            IF v_ExistingStatus = '已使用' THEN
                SET v_StatusNotExist = '码已使用';
                INSERT INTO my_table_NG (C1, C2, C3, Status)
                VALUES (p_C1, p_C2, p_C3, v_StatusNotExist);

            -- 3.2.2 如果 Status 不是 '已使用'，进行关联性检测 (标号8)
            ELSE
                SELECT COUNT(*) INTO v_Count
                FROM my_table_new
                WHERE C1 = p_C1 AND C2 = p_C2 AND C3 = p_C3;

                -- 3.2.2.1 如果能找到完整匹配的记录，插入到 my_table_old (标号9)
                IF v_Count > 0 THEN
                    -- 插入数据到 my_table_old
                    INSERT INTO my_table_old (C1, C2, C3)
                    VALUES (p_C1, p_C2, p_C3);

                    -- 更新状态为 '已使用'
                    UPDATE my_table_new
                    SET Status = '已使用'
                    WHERE C1 = p_C1 AND C2 = p_C2 AND C3 = p_C3;

                    -- 插入成功，设置输出参数为 1 (标号10)
                    SET p_InsertSuccess = 1;
                
                -- 3.2.2.2 如果没有找到完整匹配的记录，插入 '关联性错误' 到 my_table_NG (标号11)
                ELSE
                    SET v_StatusNotExist = '关联性错误';
                    INSERT INTO my_table_NG (C1, C2, C3, Status)
                    VALUES (p_C1, p_C2, p_C3, v_StatusNotExist);
                END IF;
            END IF;
        END IF;
    END IF;
END;
";
                    break;
                case 4:
                    CreateIsNGString = @"";
                    break;
                case 5:
                    CreateIsNGString = @"";
                    break;
                case 6:
                    CreateIsNGString = @"";
                    break;
            }

            ClearNewBlankString = "TRUNCATE TABLE my_table_new;";

            //加载新码包需要根据传入的码包名字动态生成
            LoadNewBagString = string.Empty;


            // 动态生成列名 C1, C2, ..., Cn
            var columns = new List<string>();
            for (int i = 1; i <= ColumnCount; i++)
            {
                columns.Add($"C{i}");
            }

            // 将列名组合成 SQL 格式
            string columnsSql = string.Join(", ", columns);

            NewBlankInpectString = $@"
            DELETE FROM my_table_new
            WHERE ({columnsSql}) IN (
                SELECT {columnsSql}
                FROM my_table_old
            );
        ";
            ulong sql_size = (ulong)PoolSize * 1024 * 1024 * 1024;
            SetPoolSizeString = $"SET GLOBAL innodb_buffer_pool_size = {sql_size}";

            GetLeagleDataString = $@"
                        SELECT {columnsSql} 
                        FROM my_table_new 
                        WHERE Status = '未下发' 
                        LIMIT 1;
                    ";

            UpdateStatusString = "";





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







    }
}
