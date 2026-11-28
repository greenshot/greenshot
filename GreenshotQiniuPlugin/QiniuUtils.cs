/*
 * Greenshot - a free and open source screenshot tool
 * Copyright (C) 2007-2016 Thomas Braun, Jens Klingen, Robin Krom
 * 
 * For more information see: http://getgreenshot.org/
 * The Greenshot project is hosted on GitHub https://github.com/greenshot/greenshot
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 1 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using Greenshot.IniFile;
using Greenshot.Plugin;
using GreenshotPlugin.Core;

using Qiniu.Http;
using Qiniu.Util;
using Qiniu.Storage;



namespace GreenshotQiniuPlugin
{
	/// <summary>
	/// A collection of Qiniu helper methods
	/// </summary>
	public static class QiniuUtils {
		private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(QiniuUtils));
        private static readonly QiniuConfiguration _config = IniConfig.GetIniSection<QiniuConfiguration>();
        public enum HttpCode
        {
            #region _PRE_

            /// <summary>
            /// 成功
            /// </summary>
            OK = 200,

            /// <summary>
            /// 部分OK
            /// </summary>
            PARTLY_OK = 298,

            /// <summary>
            /// 请求错误
            /// </summary>
            BAD_REQUEST = 400,

            /// <summary>
            /// 认证授权失败
            /// </summary>
            AUTHENTICATION_FAILED = 401,

            /// <summary>
            /// 拒绝访问
            /// </summary>
            ACCESS_DENIED = 403,

            /// <summary>
            /// 资源不存在
            /// </summary>
            OBJECT_NOT_FOUND = 404,

            /// <summary>
            /// CRC32校验失败
            /// </summary>
            CRC32_CHECK_FAILEd = 406,

            /// <summary>
            /// 上传文件大小超限
            /// </summary>
            FILE_SIZE_EXCEED = 413,

            /// <summary>
            /// 镜像回源失败
            /// </summary>
            PREFETCH_FAILED = 478,

            /// <summary>
            /// 错误网关
            /// </summary>
            BAD_GATEWAY = 502,

            /// <summary>
            /// 服务端不可用
            /// </summary>
            SERVER_UNAVAILABLE = 503,

            /// <summary>
            /// 服务端操作超时
            /// </summary>
            SERVER_TIME_EXCEED = 504,

            /// <summary>
            /// 单个资源访问频率过高
            /// </summary>
            TOO_FREQUENT_ACCESS = 573,

            /// <summary>
            /// 回调失败
            /// </summary>
            CALLBACK_FAILED = 579,

            /// <summary>
            /// 服务端操作失败
            /// </summary>
            SERVER_OPERATION_FAILED = 599,

            /// <summary>
            /// 资源内容被修改
            /// </summary>
            CONTENT_MODIFIED = 608,

            /// <summary>
            /// 文件不存在
            /// </summary>
            FILE_NOT_EXIST = 612,

            /// <summary>
            /// 文件已存在
            /// </summary>
            FILE_EXISTS = 614,

            /// <summary>
            /// 空间数量已达上限
            /// </summary>
            BUCKET_COUNT_LIMIT = 630,

            /// <summary>
            /// 空间或者文件不存在
            /// </summary>
            BUCKET_NOT_EXIST = 631,

            /// <summary>
            /// 列举资源(list)使用了非法的marker
            /// </summary>
            INVALID_MARKER = 640,

            /// <summary>
            /// 在断点续上传过程中，后续上传接收地址不正确或ctx信息已过期。
            /// </summary>
            CONTEXT_EXPIRED = 701,

            #endregion _PRE_

            #region _USR_

            /// <summary>
            /// 自定义HTTP状态码 (默认值)
            /// </summary>
            USER_UNDEF = -256,

            /// <summary>
            /// 自定义HTTP状态码 (用户取消)
            /// </summary>
            USER_CANCELED = -255,

            /// <summary>
            /// 自定义HTTP状态码 (用户暂停)
            /// </summary>
            USER_PAUSED = -254,

            /// <summary>
            /// 自定义HTTP状态码 (用户继续)
            /// </summary>
            USER_RESUMED = -253,

            /// <summary>
            /// 自定义HTTP状态码 (需要重试)
            /// </summary>
            USER_NEED_RETRY = -252,

            /// <summary>
            /// 自定义HTTP状态码 (异常或错误)
            /// </summary>
            USER_EXCEPTION = -252,

            #endregion _USR_

        }

        public static HttpResult UploadFile(Stream filestream,string fileName)
        {            
            Mac mac = new Mac(_config.AccessKey, _config.SecretKey);
            PutPolicy putPolicy = new PutPolicy();
            putPolicy.Scope = _config.Scope;
            putPolicy.SetExpires(3600);
            //putPolicy.DeleteAfterDays = 1;
            string token = Auth.CreateUploadToken(mac, putPolicy.ToJsonString());

            Config config = new Config();
            // 设置上传区域
            switch (_config.Zone)
            {
                case QiniuConfiguration.UploadZone.CN_East:
                    config.Zone = Zone.ZONE_CN_East;
                    break;
                case QiniuConfiguration.UploadZone.CN_North:
                    config.Zone = Zone.ZONE_CN_North;
                    break;
                case QiniuConfiguration.UploadZone.CN_South:
                    config.Zone = Zone.ZONE_CN_South;
                    break;
                case QiniuConfiguration.UploadZone.US_North:
                    config.Zone = Zone.ZONE_US_North;
                    break;
            }
            // 设置 http 或者 https 上传
            config.UseHttps = true;
            config.UseCdnDomains = true;
            config.ChunkSize = ChunkUnit.U512K;

            ResumableUploader target = new ResumableUploader(config);
            HttpResult result = target.UploadStream(filestream, fileName, token,null);
          
            if (result.Code != (int)HttpCode.OK)
            {
                Log.Error(result.Text);
                throw new Exception(result.Text);
            }      
                  
            return result;
        }

        /// <summary>
        /// Helper for logging
        /// </summary>
        /// <param name="nameValues"></param>
        /// <param name="key"></param>
        private static void LogHeader(IDictionary<string, string> nameValues, string key) {
			if (nameValues.ContainsKey(key)) {
				Log.InfoFormat("{0}={1}", key, nameValues[key]);
			}
		}

		/// <summary>
		/// Log the current rate-limit information
		/// </summary>
		/// <param name="response"></param>
		private static void LogRateLimitInfo(WebResponse response) {
			IDictionary<string, string> nameValues = new Dictionary<string, string>();
			foreach (string key in response.Headers.AllKeys) {
				if (!nameValues.ContainsKey(key)) {
					nameValues.Add(key, response.Headers[key]);
				}
			}
			LogHeader(nameValues, "X-RateLimit-Limit");
			LogHeader(nameValues, "X-RateLimit-Remaining");
			LogHeader(nameValues, "X-RateLimit-UserLimit");
			LogHeader(nameValues, "X-RateLimit-UserRemaining");
			LogHeader(nameValues, "X-RateLimit-UserReset");
			LogHeader(nameValues, "X-RateLimit-ClientLimit");
			LogHeader(nameValues, "X-RateLimit-ClientRemaining");

			// Update the credits in the config, this is shown in a form
			//int credits;
			//if (nameValues.ContainsKey("X-RateLimit-Remaining") && int.TryParse(nameValues["X-RateLimit-Remaining"], out credits)) {
			//	Config.Credits = credits;
			//}
		}
	}
}
