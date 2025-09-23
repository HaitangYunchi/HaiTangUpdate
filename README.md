### 这个是为 2018k 在线更新模块写的一个库
 **如果有需要的小伙伴，可以自行去 [https://2018k.cn/](https://2018k.cn/) 申请一个OpenID，然后调用我这里的方法就可以了	** 
 ```csharp
    // 首先实例化
	HaiTangUpdate.Update up = new();
        
	// 获取各种更新信息的示例方法调用
	string downloadLink = await up.GetDownloadLink("实例ID", "你的OpenID","机器码");				// 获取下载链接
	string softwareID = await up.GetSoftwareID("实例ID", "你的OpenID","机器码");					// 获取软件ID
	string versionNumber = await up.GetVersionNumber("实例ID", "你的OpenID","机器码");				// 获取版本号
	string softwareName = await up.GetSoftwareName("实例ID", "你的OpenID","机器码");				// 获取软件名称
	string versionInfo = await up.GetVersionInformation("实例ID", "你的OpenID","机器码");			// 获取版本信息
	string notice = await up.GetNotice("实例ID", "你的OpenID","机器码");							// 获取更新通知
	string visits = await up.GetNumberOfVisits("实例ID", "你的OpenID","机器码");					// 获取访问次数
	string miniVersion = await up.GetMiniVersion("实例ID", "你的OpenID","机器码");					// 获取最小版本
	string isEffective = await up.GetIsItEffective("实例ID", "你的OpenID","机器码");				// 获取是否有效
	string expirationDate = await up.GetExpirationDate("实例ID", "你的OpenID","机器码");			// 获取到期日期
	string remarks = await up.GetRemarks("实例ID", "你的OpenID","机器码");							// 获取备注
	string days = await up.GetNumberOfDays("实例ID", "你的OpenID","机器码");						// 获取天数
	string networkVerifyId = await up.GetNetworkVerificationId("实例ID", "你的OpenID","机器码");	// 获取网络验证ID
	string timestamp = await up.GetTimeStamp("实例ID", "你的OpenID","机器码");						// 获取时间戳
	string mandatoryUpdate = await up.GetMandatoryUpdate("实例ID", "你的OpenID","机器码");			// 获取强制更新状态
	string md5 = await up.GetSoftwareMd5("实例ID", "你的OpenID","机器码");							// 获取软件MD5
	string JsonEncryData = await up.GetUpade("实例ID","你的OpenID","机器码");						// 返回实例所有数据
	string CloudVar = await up.GetCloudVariables("实例ID", "你的OpenID","云端变量名称");			// 获取你的云变量（变量值）

	up.AesDecrypt("加密的data","你的OpenID");			// 返回解密后的数据
	up.AesEncrypt("待加密数据data","你的OpenID"));		// 返回加密后的数据
	up.ActivationKey("实例ID","卡密ID","机器码");		// 激活软件
	await up.MessageSend("实例ID", "要发送的消息");	//发送消息

	up.GetMachineCode();	// 获取机器码 cpu+主板 返回20位机器码，格式：XXXXX-XXXXX-XXXXX-XXXXX
	await up.CreateNetworkAuthentication("卡密天数", "卡密备注","实例ID","你的OpenID");	// 创建卡密

	var response = await up.GetNetworkCode(实例ID, OpenID);		// 获取验证码
	await up.ReplaceBind(实例ID, OpenID,卡密ID, 机器码);			// 卡密换绑（貌似有点小bug，待定）
	await up.CustomerLogon(实例ID,OpenID, 邮箱, 密码);				// 用户注册 返回布尔值
	await up.CustomerLogon(实例ID,OpenID, 邮箱, 密码);				// 用户登录 返回布尔值
	await up.Recharge(实例ID,OpenID, 邮箱, 密码,卡密ID);			// 充值
	await up.GetUserInfo(实例ID,OpenID, 邮箱, 密码);				// 获取用户信息
	await up.GetUserId(实例ID,OpenID, 邮箱, 密码);					// 获取用户ID
	await up.GetUserAvatar(实例ID,OpenID, 邮箱, 密码);				// 获取用户头像地址
	await up.GetUserNickname(实例ID,OpenID, 邮箱, 密码);			// 获取用户昵称
	await up.GetUserEmail(实例ID,OpenID, 邮箱, 密码);				// 获取用户邮箱
	await up.GetUserBalance(实例ID,OpenID, 邮箱, 密码);			// 获取账户余额（剩余时长）
	await up.GetUserLicense(实例ID,OpenID, 邮箱, 密码);			// 获取授权信息
	await up.GetUserTimeCrypt(实例ID,OpenID, 邮箱, 密码);			// 验证登录时间戳

	新增：
	更新了日志记录功能 ，可以记录调用日志到本地文件，方便调试
	首先using HaiTangUpdate; 
	在需要记录日志的代码中添加如下代码：
	Logger.Log($"程序启动", Logger.LogLevel.INFO);				// 其中 INFO 可以替换为 INFO WARN ERROR
	Logger.Log($"程序启动已完成", Logger.LogLevel.INFO);			// 其中 INFO 可以替换为 INFO WARN ERROR
	ShaHasher.Sha256("待哈希的数据");	// 返回64位字符串哈希后的数据
	ShaHasher.Sha512("待哈希的数据");	// 返回128位字符串哈希后的数据


    // 获取卡密剩余时间（类型long  返回值：永久-1，过期0，未注册1，其他返回时间戳）
	long timestamp = up.GetRemainingUsageTime("实例ID", "你的OpenID","机器码");      
        

        // 版本检查使用示例
		
        try
        {
			// 获取服务器版本信息
			string serverVersion = await Task.Run(() => up.GetVersionNumber(实例ID,OpenID));
            Version currentVersion = assembly.GetName().Version;	// 获取当前程序版本
            Version latestVersion = new(serverVersion);				// 转换服务端版本号为标准版本号

            if (currentVersion < latestVersion)
            {
                string downloadUrl = await up.GetDownloadLink("实例ID", "你的OpenID");
                string updateInfo =  await up.GetVersionInformation("实例ID", "你的OpenID");
                Console.WriteLine($"有新版本可用: {latestVersion}");
                Console.WriteLine($"更新信息: {updateInfo}");
                Console.WriteLine($"下载地址: {downloadUrl}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"更新检查失败: {ex.Message}");
        }
        
        
        // 卡密剩余时间调用示例
        try
        {
			var timestamp = await up.GetRemainingUsageTime("实例ID", "你的OpenID","机器码");
			if (timestamp == -1)
			{
				Console.WriteLine("永久");
			}
			else if (timestamp == 0)
			{
				Console.WriteLine("已过期");
			}
				else if (timestamp == 1)
			{
				Console.WriteLine("未激活");
			}
			else
			{
				TimeSpan timeSpan = TimeSpan.FromMilliseconds(timestamp);
				int days = timeSpan.Days;
				int hours = timeSpan.Hours;
				int minutes = timeSpan.Minutes;
				int seconds = timeSpan.Seconds;
				Console.WriteLine($"{days}天{hours}小时{minutes}分钟{seconds}秒");
				
				//可以直接写为：Console.WriteLine($"{timeSpan.Days}天{timeSpan.hours}小时{timeSpan.minutes}分钟{timeSpan.seconds}秒");
			}
        }
        catch(Exception ex)
        {
        Console.WriteLine($"获取卡密剩余时间失败: {ex.Message}");
        }
        

	更新了自动检测api地址，优先使用默认地址，默认地址不通的时候在枚举其他地址，并把检测到健康的地址缓存起来（缓存时间5分钟）
	private const string DefaultApiUrl = "http://api.2018k.cn";
	private static string OpenApiUrl = DefaultApiUrl;
	// 可用的API地址列表，用于故障转移
	private static readonly string[] ApiAddressList =
	{
		"api.2018k.cn",
		"api2.2018k.cn",
		"api3.2018k.cn",
		"api4.2018k.cn"
	};

```	


新增Json转换类
方法列表：

ListToJson<T>(IList<T> list)

ListToJson<T>(IList<T> list, string jsonName)

ToJson(object jsonObject)

ToJson(IEnumerable array)

ToArrayString(IEnumerable array)

ToJson(DataSet dataSet)

ToJson(DataTable dt) 和 ToJson(DataTable dt, string jsonName)

ToJson(DbDataReader dataReader)

详细调用：

ListToJson - List转换成Json（两个重载方法）

方法1：无参重载
```csharp
// 定义实体类
public class Person
{
    public string Name { get; set; }
    public int Age { get; set; }
    public bool IsStudent { get; set; }
    public DateTime BirthDate { get; set; }
}

// 调用示例
List<Person> personList = new List<Person>
{
    new Person { Name = "张三", Age = 25, IsStudent = false, BirthDate = new DateTime(1998, 5, 10) },
    new Person { Name = "李四", Age = 18, IsStudent = true, BirthDate = new DateTime(2005, 8, 15) }
};

// 使用无参重载，自动使用类名作为JSON键名
string json1 = ConvertJson.ListToJson(personList);
// 结果: {"Person":[{"Name":"张三","Age":25,"IsStudent":false,"BirthDate":"1998-05-10"},{"Name":"李四","Age":18,"IsStudent":true,"BirthDate":"2005-08-15"}]}
```
方法2：带jsonName参数
```csharp
// 使用自定义JSON键名
string json2 = ConvertJson.ListToJson(personList, "Employees");
// 结果: {"Employees":[{"Name":"张三","Age":25,"IsStudent":false,"BirthDate":"1998-05-10"},{"Name":"李四","Age":18,"IsStudent":true,"BirthDate":"2005-08-15"}]}

// 空键名时使用类名
string json3 = ConvertJson.ListToJson(personList, "");
// 结果: {"Person":[{"Name":"张三","Age":25,"IsStudent":false,"BirthDate":"1998-05-10"},{"Name":"李四","Age":18,"IsStudent":true,"BirthDate":"2005-08-15"}]}
```
2. ToJson(object) - 对象转换为Json
```csharp
// 单个对象转换
Person person = new Person 
{ 
    Name = "王五", 
    Age = 30, 
    IsStudent = false, 
    BirthDate = new DateTime(1993, 12, 20) 
};

string json = ConvertJson.ToJson(person);
// 结果: {"Name":"王五","Age":30,"IsStudent":false,"BirthDate":"1993-12-20"}

// 包含特殊字符的测试
Person specialPerson = new Person 
{ 
    Name = "John \"The Boss\"",  // 包含引号
    Age = 35, 
    IsStudent = false,
    BirthDate = DateTime.Now
};

string specialJson = ConvertJson.ToJson(specialPerson);
// 结果: {"Name":"John \"The Boss\"","Age":35,"IsStudent":false,"BirthDate":"2025-09-23"}
```
3. ToJson(IEnumerable) - 对象集合转换Json
```csharp
// 对象集合转换
List<Person> people = new List<Person>
{
    new Person { Name = "赵六", Age = 22, IsStudent = true },
    new Person { Name = "钱七", Age = 35, IsStudent = false }
};

string json = ConvertJson.ToJson(people);
// 结果: [{"Name":"赵六","Age":22,"IsStudent":true},{"Name":"钱七","Age":35,"IsStudent":false}]

// 数组也可以
Person[] personArray = new Person[]
{
    new Person { Name = "孙八", Age = 28, IsStudent = false },
    new Person { Name = "周九", Age = 19, IsStudent = true }
};

string arrayJson = ConvertJson.ToJson(personArray);
// 结果: [{"Name":"孙八","Age":28,"IsStudent":false},{"Name":"周九","Age":19,"IsStudent":true}]
```
4. ToArrayString - 普通集合转换Json
```csharp
// 字符串集合
List<string> fruits = new List<string> { "苹果", "香蕉", "橙子" };
string fruitsJson = ConvertJson.ToArrayString(fruits);
// 结果: ["苹果","香蕉","橙子"]

// 数值集合
int[] numbers = { 1, 2, 3, 4, 5 };
string numbersJson = ConvertJson.ToArrayString(numbers);
// 结果: [1,2,3,4,5]

// 混合类型（会被转换为字符串）
ArrayList mixedList = new ArrayList { "text", 123, true, 45.67 };
string mixedJson = ConvertJson.ToArrayString(mixedList);
// 结果: ["text","123","True","45.67"]
```
5. ToJson(DataSet) - DataSet转换为Json
```csharp
// 创建DataSet示例
DataSet dataSet = new DataSet("CompanyData");

// 第一个表：员工表
DataTable employeesTable = new DataTable("Employees");
employeesTable.Columns.Add("ID", typeof(int));
employeesTable.Columns.Add("Name", typeof(string));
employeesTable.Columns.Add("Department", typeof(string));
employeesTable.Columns.Add("Salary", typeof(decimal));
employeesTable.Rows.Add(1, "张三", "技术部", 8000.00m);
employeesTable.Rows.Add(2, "李四", "销售部", 6500.00m);

// 第二个表：部门表
DataTable departmentsTable = new DataTable("Departments");
departmentsTable.Columns.Add("DeptID", typeof(int));
departmentsTable.Columns.Add("DeptName", typeof(string));
departmentsTable.Columns.Add("Manager", typeof(string));
departmentsTable.Rows.Add(101, "技术部", "王总监");
departmentsTable.Rows.Add(102, "销售部", "李经理");

dataSet.Tables.Add(employeesTable);
dataSet.Tables.Add(departmentsTable);

string dataSetJson = ConvertJson.ToJson(dataSet);
// 结果: {"Employees":[{"ID":1,"Name":"张三","Department":"技术部","Salary":8000.00},{"ID":2,"Name":"李四","Department":"销售部","Salary":6500.00}],"Departments":[{"DeptID":101,"DeptName":"技术部","Manager":"王总监"},{"DeptID":102,"DeptName":"销售部","Manager":"李经理"}]}
```
6. ToJson(DataTable) - DataTable转换为Json（两个重载方法）
方法1：无表名参数
```csharp
DataTable dt = new DataTable("Products");
dt.Columns.Add("ProductID", typeof(int));
dt.Columns.Add("ProductName", typeof(string));
dt.Columns.Add("Price", typeof(decimal));
dt.Columns.Add("InStock", typeof(bool));
dt.Rows.Add(1, "笔记本电脑", 5999.99m, true);
dt.Rows.Add(2, "智能手机", 2999.99m, false);
dt.Rows.Add(3, "平板电脑", 1999.99m, true);

string json1 = ConvertJson.ToJson(dt);
// 结果: [{"ProductID":1,"ProductName":"笔记本电脑","Price":5999.99,"InStock":true},{"ProductID":2,"ProductName":"智能手机","Price":2999.99,"InStock":false},{"ProductID":3,"ProductName":"平板电脑","Price":1999.99,"InStock":true}]
```
方法2：带表名参数
```csharp
string json2 = ConvertJson.ToJson(dt, "商品列表");
// 结果: {"商品列表":[{"ProductID":1,"ProductName":"笔记本电脑","Price":5999.99,"InStock":true},{"ProductID":2,"ProductName":"智能手机","Price":2999.99,"InStock":false},{"ProductID":3,"ProductName":"平板电脑","Price":1999.99,"InStock":true}]}

// 空表名时使用DataTable的表名
string json3 = ConvertJson.ToJson(dt, "");
// 结果: {"Products":[{"ProductID":1,"ProductName":"笔记本电脑","Price":5999.99,"InStock":true},{"ProductID":2,"ProductName":"智能手机","Price":2999.99,"InStock":false},{"ProductID":3,"ProductName":"平板电脑","Price":1999.99,"InStock":true}]}
```
B站 海棠云螭：[https://space.bilibili.com/3493128132626725](https://space.bilibili.com/3493128132626725)

c#开发的获取程序版本及更新信息对比的动态链接库，采用.NET 8.0 框架编写，低于.NET 8.0 的不能使用哦