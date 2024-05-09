using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Mail;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
namespace QTool
{

	public static class QMailTool
    {
	
		public static void Send(QMailAccount account, string toAddres, string title, string messageInfo, params string[] files)
		{
			_=SendAsync(account, toAddres, title, messageInfo, files);
		}
		
		public static async Task<bool> SendAsync(QMailAccount account, string toAddres, string title, string messageInfo ,params string[] files)
        {
			try
			{
				using (var client = new SmtpClient(account.smtpServer))
				{
					client.Credentials = new System.Net.NetworkCredential(account.account, account.password);
					client.EnableSsl = true;
					using (var message = new MailMessage())
					{
						message.From = new MailAddress(account.account);
						message.To.Add(toAddres);
						message.IsBodyHtml = true;
						message.BodyEncoding = System.Text.Encoding.UTF8;
						message.Subject = title;
						message.Body = messageInfo;
						foreach (var filePath in files)
						{
							message.Attachments.Add(new Attachment(filePath));
						}
						await client.SendMailAsync(message);
						QDebug.Log("发送邮件成功 " + message.Subject + " " + message.Body.ToSizeString() + " \n" + message.Body);
						return true;
					}
				}
			}
			catch (Exception e)
			{
				Debug.LogError("发送邮件出错：\n"+e+"\n"+title+"\n"+messageInfo);
				return false;
			}
        }
		static async Task<string[]> CommondCheckReadLine(this StreamWriter writer,string command,StreamReader reader)
		{
			await writer.WriteLineAsync(command);
			return await reader.CheckReadLine(command);
		}
		static async Task<string[]> CheckReadLine(this StreamReader reader,string checkFlag)
		{
			var info = await reader.ReadLineAsync();
			if (info == null)
			{
				await reader.ReadLineAsync();
			}
			if (info != null && info.StartsWith("+OK"))
			{
				var infos = info.Split(' ');
				return infos; 
			}
			else
			{
				Debug.LogError(checkFlag + "读取出错 " + info);
				throw new Exception(checkFlag+"读取出错 " +info);
			}
		}
		static async Task<QMailInfo> ReceiveEmail(StreamWriter writer, StreamReader reader, int index, int countIndex = -1)
		{
			string Id = "";
			if (index == countIndex)
			{
				Id = (await writer.CommondCheckReadLine("UIDL " + index, reader))[2];
				QDebug.Log("LastID:"+index+"[" + Id+"]");
			}
			await writer.WriteLineAsync("RETR " + index);
			var size = int.Parse((await reader.CheckReadLine("RETR " + index))[1]).ToSizeString();
			using (var infoWriter = new StringWriter())
			{
				string result = null;
				while ((result = await reader.ReadLineAsync()) != ".")
				{
					infoWriter.Write( result + "\n");
				}
				var mail = new QMailInfo(infoWriter.ToString(), index, Id);
				return mail;
			}
		}
		
		public static async Task ReceiveRemailAsync(QMailAccount account, int startIndex, int endIndex, Action<QMailInfo> callBack,int threadCount=5)
		{
			QDictionary<int, QMailInfo> mailList = new QDictionary<int, QMailInfo>();
			var startTime = DateTime.Now;
			if (startIndex >endIndex) {
				QDebug.Log("无新邮件");
				return;
			}
			if (threadCount <= 0)
			{
				threadCount = 1;
			}
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				UnityEditor.EditorUtility.DisplayProgressBar("开启接收邮件线程", "开启线程" + startIndex, 0.4f);
			}
#endif
			var taskList = new List<Task>();
			QDebug.Log("开始接收邮件" + startIndex + " -> " + endIndex+" ...");
			for (int i = 0; i < threadCount; i++)
			{
				taskList.Add( ReceiveRemail(account, startIndex + i, endIndex, mailList, threadCount));
			}
			foreach (var task in taskList)	
			{
				await task;
			}

			QDebug.Log("接收邮件" + startIndex + " -> " + endIndex+ " 完成 用时: " + (DateTime.Now-startTime).ToString("hh\\:mm\\:ss") );
			QDebug.Log("开始读取邮件" + startIndex + " -> " + endIndex + " ...");
			await QTask.Wait(0.1f,true);
			startTime = DateTime.Now;
			for (var i = startIndex; i <= endIndex; i++)
			{
				var mail = mailList[i];
				try
				{

					if (mail == null)
					{
						throw new Exception("邮件为空");
					}
#if UNITY_EDITOR
					if (!Application.isPlaying)
					{
						UnityEditor.EditorUtility.DisplayProgressBar("解析邮件", i + "/" + endIndex + " " + mail.Subject, (i - startIndex) * 1f / (endIndex - startIndex));
					}
#endif
					callBack(mail);
				}
				catch (Exception e)
				{
					Debug.LogError("读取邮件出错" + i + "/" + endIndex + "：\n" + e);
				}
			}
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				UnityEditor.EditorUtility.ClearProgressBar();
			}
#endif
			QDebug.Log("读取邮件 " + startIndex + " -> " + endIndex+ " 完成 用时: " + (DateTime.Now - startTime).ToString("hh\\:mm\\:ss") );
			await QTask.Wait(0.1f, true);
		}
		static async Task ReceiveRemail(QMailAccount account, int startIndex, int endIndex, QDictionary<int, QMailInfo> mailList, int threadCount =1)
		{
			if (startIndex >endIndex)
			{
				return;
			}
			using (TcpClient clientSocket = new TcpClient())
			{
				clientSocket.Connect(account.popServer, 995);
				//建立SSL连接
				using (SslStream stream = new SslStream(clientSocket.GetStream(), false, (object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors errors) => { return true; }))
				{

					stream.AuthenticateAsClient(account.popServer);
					using (StreamReader reader = new StreamReader(stream, Encoding.Default, true))
					{
						using (StreamWriter writer = new StreamWriter(stream))
						{
							writer.AutoFlush = true;
							try
							{
								await reader.CheckReadLine("SSL连接");
								await writer.CommondCheckReadLine("USER " + account.account, reader);
								await writer.CommondCheckReadLine("PASS " + account.password, reader);
								for (var i = startIndex; i <= endIndex; i+=threadCount)
								{
									var mail = await ReceiveEmail(writer, reader, i, endIndex);
									lock (mailList)
									{
										mailList[i] = mail;
									}
#if UNITY_EDITOR
									if (!Application.isPlaying)
									{
										UnityEditor.EditorUtility.DisplayProgressBar("接收邮件 线程" + startIndex, i + "/" + endIndex + " " + mail.Subject, (i - startIndex) * 1f / (endIndex - startIndex));
									}
#endif
								}
							//	QDebug.Log("接收结束 "+startIndex+" 线程");

							}
							catch (Exception e)
							{
								throw new Exception("邮件接收出错：", e);
							}
						}
					}
				}
				clientSocket.Close();
			}
		}
		public static async Task<bool> FreshEmails(QMailAccount account, Action<QMailInfo> callBack, QMailInfo lastMail,int maxCount=0)
		{
			bool loadOver = true;
			using (TcpClient clientSocket = new TcpClient())
			{
				clientSocket.Connect(account.popServer, 995);
				//建立SSL连接
				using (SslStream stream = new SslStream(clientSocket.GetStream(), false, (object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors errors) => { return true; }))
				{

					stream.AuthenticateAsClient(account.popServer);
					using (StreamReader reader = new StreamReader(stream, Encoding.Default, true))
					{
						using (StreamWriter writer = new StreamWriter(stream))
						{
							writer.AutoFlush = true;

							try
							{

								await reader.CheckReadLine("SSL连接");
#if UNITY_EDITOR
								if (!Application.isPlaying)
								{
									UnityEditor.EditorUtility.DisplayProgressBar("接收邮件信息", "SSL连接成功", 0.1f);
								}
#endif

								await writer.CommondCheckReadLine("USER " + account.account, reader);
								await writer.CommondCheckReadLine("PASS " + account.password, reader);
#if UNITY_EDITOR
								if (!Application.isPlaying)
								{
									UnityEditor.EditorUtility.DisplayProgressBar("接收邮件信息", "SSL账户登录成功", 0.2f);
								}
#endif
								var infos = await writer.CommondCheckReadLine("STAT", reader);
								var endIndex = int.Parse(infos[1]);
								QDebug.Log("邮件总数：" + endIndex + " 总大小：" + long.Parse(infos[2]).ToSizeString());
								int startIndex = 1;
								if (!string.IsNullOrWhiteSpace(lastMail?.Id)) 
								{
									QDebug.Log("上一封邮件：" +lastMail.Index+"["+lastMail.Id+"] "+ lastMail.Date);
									if (await writer.IdCheck(lastMail.Index, lastMail.Id, reader))
									{
										startIndex = lastMail.Index + 1;
									}
									else
									{
										for (var i = lastMail.Index - 1; i >= 1; i--)
										{
											if (await writer.IdCheck(i, lastMail.Id, reader))
											{
												startIndex = i+ 1;
												lastMail.Index = i;
												break;
											}
										}
									}
								}
								if (maxCount > 0 && endIndex - startIndex > maxCount - 1)
								{
									loadOver = false;
									endIndex = startIndex + maxCount - 1;
								}
#if UNITY_EDITOR
								if (!Application.isPlaying)
								{
									UnityEditor.EditorUtility.DisplayProgressBar("接收邮件信息", "获取起始邮件索引成功", 0.3f);
								}
#endif
								await ReceiveRemailAsync(account, startIndex, endIndex, callBack,20);
#if UNITY_EDITOR
								if (!Application.isPlaying)
								{
									UnityEditor.EditorUtility.ClearProgressBar();
								}
#endif
							}
							catch (Exception e)
							{
								Debug.LogError("邮件读取出错：" + e);
							}
							clientSocket.Close();
						}
					}
				}
			}
			return loadOver;
		}
		public static async Task<bool> IdCheck(this StreamWriter writer ,long index,string Id, StreamReader reader)
		{
			var task = writer.CommondCheckReadLine("UIDL " + index, reader);
			try
			{
				return (await task)[2] == Id;
			}
			catch (Exception )
			{
				return false;
			}
		}
	}
	
	[System.Serializable]
	public class QMailAccount
	{
		public string account;
		public string password;
		public string popServer;
		public string smtpServer;
		public void Init()
		{
			if (string.IsNullOrWhiteSpace(account)) return;
			this.popServer = string.IsNullOrEmpty(this.popServer) ? GetServer(account, "pop.") : this.popServer;
			this.smtpServer = string.IsNullOrEmpty(this.smtpServer) ? GetServer(account, "smtp.") : this.smtpServer;
		}
		static string GetServer(string emailAddress, string start)
		{
			if (emailAddress.IndexOf('@') < 0)
			{
				throw new Exception("不支持邮箱 " + emailAddress);
			}
			return start + "." + emailAddress.Substring(emailAddress.IndexOf("@") + 1);
		}
		public bool InitOver
		{
			get
			{
				return !string.IsNullOrEmpty(account) && !string.IsNullOrEmpty(password) && !string.IsNullOrEmpty(popServer) && !string.IsNullOrEmpty(smtpServer);
			}
		}
	}
	public class QMailInfo
	{

		public string Subject;
		public string From;
		public string Cc;
		public string To;
		public string Date;
		public string Body;
		public int Index;
		public string Id;
		public QMailInfo()
		{

		}
		const string AVGKey = @"X-Antivirus-Status: Clean";
		public QMailInfo(string mailStr, int Index, string Id)
		{
			this.Index = Index;
			this.Id = Id;
			
	
			Subject = GetString(mailStr, "Subject: ");
			From = GetString(mailStr, "From: ").Trim();
			Cc = GetString(mailStr, "Cc: ").Trim(); 
			To = GetString(mailStr, "To: ").Trim();
			Date= GetString(mailStr, "Date: ");
			try
			{
				if (GetString(mailStr, "Content-Type: ") == "text/html; charset=utf-8")
				{
					if (GetString(mailStr, "Content-Transfer-Encoding: ") == "base64")
					{
						var info = mailStr.GetBlockValue("Content-Transfer-Encoding: base64", "------=").Trim();
						if (info.Contains(AVGKey))
						{
							Body = ParseBase64String(info.SplitEndString(AVGKey).Trim());
						}
						else
						{
							Body = ParseBase64String(info, (data) => data.SplitStartString("="));
						}
						if (string.IsNullOrWhiteSpace(Body))
						{
							Debug.LogWarning("读取 [" + Subject + "]内容为空 " + Date);
						}
						return;
					}
				}
			}
			catch (Exception e)
			{
				Debug.LogWarning("解析邮件[" + Subject + "][" + Date + "]内容出错"+ e);
			}
		

			//Body = "未能解析格式：\n" + mailStr;
		}
		public override string ToString()
		{
			return "【" + Subject + "】from：" + From +"  "+Date+ "\n" + Body;
		}


		private static string ParseBase64String(string base64Str,Func<string,string> errorFunc=null)
		{
			try
			{
				return Encoding.UTF8.GetString(Convert.FromBase64String(base64Str));
			}
			catch (Exception e)
			{
				if (errorFunc != null)
				{
					return ParseBase64String(errorFunc(base64Str));
				}
				else
				{
					GUIUtility.systemCopyBuffer = base64Str;
					Debug.LogError("错误数据:\n" + e + "\n" + base64Str);
					return "";
				}
			}
		}
		private static string GetString(string SourceString, string Key)
		{
			var startIndex =string.IsNullOrEmpty(Key)?0:SourceString.IndexOf('\n'+Key);
			if (startIndex >= 0)
			{
				startIndex += Key.Length+1;
				var endIndex = SourceString.IndexOf('\n', startIndex);
				var info = SourceString.Substring(startIndex, endIndex - startIndex);
				return  CheckString(info);
			}
			else
			{
				return "";
			}
		}


		private static string CheckString(string SourceString)
		{
			if (SourceString.Contains("=?"))
			{
				if (SourceString.Contains("\"=?"))
				{
					SourceString = SourceString.Replace("\"=?", "=?").Replace("?=\"", "?=");
				}
				var start = SourceString.IndexOf("=?");
				var end = SourceString.LastIndexOf("?=");
				var midStr = SourceString.Substring(start, end - start + 2);
				var newInfo = Attachment.CreateAttachmentFromString("", midStr).Name;
				if (midStr.Contains(newInfo))
				{
					if (midStr.ToLower().StartsWith("=?utf-8?b?"))
					{
						newInfo = ParseBase64String(midStr.Substring(10, midStr.Length - 12));
					}
					else
					{
						Debug.LogError("[" + midStr + "]  =>  " + newInfo);
					}
				}

				return SourceString.Substring(0, start) + newInfo + SourceString.Substring(end + 2);
			}
			else
			{
				return SourceString;
			}
		}
	
	}
}
