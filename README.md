	// 首先实例化
	HaiTangUpdate.Update up = new HaiTangUpdate.Update();
        
	// 获取各种更新信息的示例方法调用
	string downloadLink = await up.GetDownloadLink("2018K程序实例ID", "你的OpenID","机器码");              // 获取下载链接
	string softwareID = await up.GetSoftwareID("2018K程序实例ID", "你的OpenID","机器码");                  // 获取软件ID
	string versionNumber = await up.GetVersionNumber("2018K程序实例ID", "你的OpenID","机器码");           // 获取版本号
	string softwareName = await up.GetSoftwareName("2018K程序实例ID", "你的OpenID","机器码");             // 获取软件名称
	string versionInfo = await up.GetVersionInformation("2018K程序实例ID", "你的OpenID","机器码");        // 获取版本信息
	string notice = await up.GetNotice("2018K程序实例ID", "你的OpenID","机器码");                        // 获取更新通知
	string visits = await up.GetNumberOfVisits("2018K程序实例ID", "你的OpenID","机器码");                // 获取访问次数
	string miniVersion = await up.GetMiniVersion("2018K程序实例ID", "你的OpenID","机器码");              // 获取最小版本
	string isEffective = await up.GetIsItEffective("2018K程序实例ID", "你的OpenID","机器码");            // 获取是否有效
	string expirationDate = await up.GetExpirationDate("2018K程序实例ID", "你的OpenID","机器码");        // 获取到期日期
	string remarks = await up.GetRemarks("2018K程序实例ID", "你的OpenID","机器码");                      // 获取备注
	string days = await up.GetNumberOfDays("2018K程序实例ID", "你的OpenID","机器码");                    // 获取天数
	string networkVerifyId = await up.GetNetworkVerificationId("2018K程序实例ID", "你的OpenID","机器码"); // 获取网络验证ID
	string timestamp = await up.GetTimeStamp("2018K程序实例ID", "你的OpenID","机器码");                  // 获取时间戳
	string mandatoryUpdate = await up.GetMandatoryUpdate("2018K程序实例ID", "你的OpenID","机器码");      // 获取强制更新状态
	string md5 = await up.GetSoftwareMd5("2018K程序实例ID", "你的OpenID","机器码");                      // 获取软件MD5
	string JsonEncryData = await up.GetUpade("2018K程序实例ID","你的OpenID","机器码"); //返回你的data数据
	string CloudVar = await up.GetCloudVariables("2018K程序实例ID", "你的OpenID","云端变量名称"); // 获取你的云变量（变量值）
	up.AesDecrypt("加密的data","你的OpenID");//返回解密后的数据
	up.AesEncrypt("待加密数据data","你的OpenID"));//返回加密后的数据
	up.ActivationKey("2018K程序实例ID","卡密ID","机器码");//激活软件
	await up.MessageSend("2018K程序实例ID", "要发送的消息");//发送消息
	up.GetMachineCode();// 获取机器码 cpu+主板 返回20位机器码，格式：XXXXX-XXXXX-XXXXX-XXXXX
	await up.CreateNetworkAuthentication("卡密天数", "卡密备注","2018K程序实例ID","你的OpenID");//创建卡密
	long timestamp = up.GetRemainingUsageTime("2018K程序实例ID", "你的OpenID","机器码");      // 获取卡密剩余时间（类型long  返回值：永久-1，过期0，未注册1，其他返回时间戳）
        
        // 使用示例
        try
        {
            // 假设我们要检查更新
            string currentVersion = "1.0.0"; // 当前程序版本
            string latestVersion =  await up.GetVersionNumber("2018K程序实例ID", "你的OpenID");
            
            if (latestVersion != currentVersion)
            {
                string downloadUrl =  await up.GetDownloadLink("2018K程序实例ID", "你的OpenID");
                string updateInfo =  await up.GetVersionInformation("2018K程序实例ID", "你的OpenID");
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
			var timestamp = await up.GetRemainingUsageTime("2018K程序实例ID", "你的OpenID","机器码");
			if (timestamp == 0)
			{
				Console.WriteLine("已过期");
			}
			else if (timestamp == 1)
			{
				Console.WriteLine("未激活");
			}
				else if (timestamp == -1)
			{
				Console.WriteLine("永久");
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
        
	目前就差换绑没做，这段时间没空，等有空了再更新吧，本次更新是更新了方法调用参数，加了机器码的调用
	更新了自动检测api地址，优先使用默认地址，默认地址不通的时候在枚举其他地址，并把检测到健康的地址缓存起来（缓存时间5分钟）
	private const string DefaultApiUrl = "http://api.2018k.cn/v3/";
	private static string OpenApiUrl = DefaultApiUrl;
	// 可用的API地址列表，用于故障转移
	private static readonly string[] ApiAddressList =
	{
		"http://api.2018k.cn/v3/",
		"http://api2.2018k.cn/v3/",
		"http://api3.2018k.cn/v3/",
		"http://api4.2018k.cn/v3/"
	};

	B站 海棠云螭：https://space.bilibili.com/3493128132626725

	c#开发的获取程序版本及更新信息对比的动态链接库，当然易语言也可以调用这个库，详细的请自行搜索使用方法	// 首先实例化
	HaiTangUpdate.Update up = new HaiTangUpdate.Update();
        
	// 获取各种更新信息的示例方法调用
	string downloadLink = await up.GetDownloadLink("2018K程序实例ID", "你的OpenID","机器码");              // 获取下载链接
	string softwareID = await up.GetSoftwareID("2018K程序实例ID", "你的OpenID","机器码");                  // 获取软件ID
	string versionNumber = await up.GetVersionNumber("2018K程序实例ID", "你的OpenID","机器码");           // 获取版本号
	string softwareName = await up.GetSoftwareName("2018K程序实例ID", "你的OpenID","机器码");             // 获取软件名称
	string versionInfo = await up.GetVersionInformation("2018K程序实例ID", "你的OpenID","机器码");        // 获取版本信息
	string notice = await up.GetNotice("2018K程序实例ID", "你的OpenID","机器码");                        // 获取更新通知
	string visits = await up.GetNumberOfVisits("2018K程序实例ID", "你的OpenID","机器码");                // 获取访问次数
	string miniVersion = await up.GetMiniVersion("2018K程序实例ID", "你的OpenID","机器码");              // 获取最小版本
	string isEffective = await up.GetIsItEffective("2018K程序实例ID", "你的OpenID","机器码");            // 获取是否有效
	string expirationDate = await up.GetExpirationDate("2018K程序实例ID", "你的OpenID","机器码");        // 获取到期日期
	string remarks = await up.GetRemarks("2018K程序实例ID", "你的OpenID","机器码");                      // 获取备注
	string days = await up.GetNumberOfDays("2018K程序实例ID", "你的OpenID","机器码");                    // 获取天数
	string networkVerifyId = await up.GetNetworkVerificationId("2018K程序实例ID", "你的OpenID","机器码"); // 获取网络验证ID
	string timestamp = await up.GetTimeStamp("2018K程序实例ID", "你的OpenID","机器码");                  // 获取时间戳
	string mandatoryUpdate = await up.GetMandatoryUpdate("2018K程序实例ID", "你的OpenID","机器码");      // 获取强制更新状态
	string md5 = await up.GetSoftwareMd5("2018K程序实例ID", "你的OpenID","机器码");                      // 获取软件MD5
	string JsonEncryData = await up.GetUpade("2018K程序实例ID","你的OpenID","机器码"); //返回你的data数据
	string CloudVar = await up.GetCloudVariables("2018K程序实例ID", "你的OpenID","云端变量名称"); // 获取你的云变量（变量值）
	up.AesDecrypt("加密的data","你的OpenID");//返回解密后的数据
	up.AesEncrypt("待加密数据data","你的OpenID"));//返回加密后的数据
	up.ActivationKey("2018K程序实例ID","卡密ID","机器码");//激活软件
	await up.MessageSend("2018K程序实例ID", "要发送的消息");//发送消息
	up.GetMachineCode();// 获取机器码 cpu+主板 返回20位机器码，格式：XXXXX-XXXXX-XXXXX-XXXXX
	await up.CreateNetworkAuthentication("卡密天数", "卡密备注","2018K程序实例ID","你的OpenID");//创建卡密
	long timestamp = up.GetRemainingUsageTime("2018K程序实例ID", "你的OpenID","机器码");      // 获取卡密剩余时间（类型long  返回值：永久-1，过期0，未注册1，其他返回时间戳）
        
        // 使用示例
        try
        {
            // 假设我们要检查更新
            string currentVersion = "1.0.0"; // 当前程序版本
            string latestVersion =  await up.GetVersionNumber("2018K程序实例ID", "你的OpenID");
            
            if (latestVersion != currentVersion)
            {
                string downloadUrl =  await up.GetDownloadLink("2018K程序实例ID", "你的OpenID");
                string updateInfo =  await up.GetVersionInformation("2018K程序实例ID", "你的OpenID");
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
			var timestamp = await up.GetRemainingUsageTime("2018K程序实例ID", "你的OpenID","机器码");
			if (timestamp == 0)
			{
				Console.WriteLine("已过期");
			}
			else if (timestamp == 1)
			{
				Console.WriteLine("未激活");
			}
				else if (timestamp == -1)
			{
				Console.WriteLine("永久");
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
        
	目前就差换绑没做，这段时间没空，等有空了再更新吧，本次更新是更新了方法调用参数，加了机器码的调用
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

	B站 海棠云螭：[https://space.bilibili.com/3493128132626725](https://space.bilibili.com/3493128132626725)

	c#开发的获取程序版本及更新信息对比的动态链接库，当然易语言也可以调用这个库，详细的请自行搜索使用方法