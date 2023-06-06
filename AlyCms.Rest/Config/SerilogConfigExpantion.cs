using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Exceptions;
using System;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace AlyCms.Rest
{
    public static class SerilogConfigExpantion
    {
        public static IHostBuilder UseSerilog(this IHostBuilder buider)
        {
            return buider.UseSerilog((context, config) =>
            {
                string pathFormat = Path.Combine("logs", @"{Date}.log");
                string outputTemplatePlain = @"{Timestamp:yyyy-MM-dd HH:mm:ss.fff} -{Level}-->{NewLine}{SourceContext}{NewLine}{Message:lj}{NewLine}";
                string outputTemplateDetail = @"{Timestamp:yyyy-MM-dd HH:mm:ss.fff} -{Level}-->{NewLine}{SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}";

                config.MinimumLevel.Verbose()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .WriteTo.Debug(LogEventLevel.Verbose, outputTemplateDetail)
                    .WriteTo.Logger(cfg =>
                        cfg.Filter.ByIncludingOnly(m => m.Level < LogEventLevel.Warning)
                           .WriteTo.Async(c =>
                               c.File(pathFormat, //文件的路径.
                                    LogEventLevel.Warning, //通过接收器的事件的最低级别,指定 <paramref name="levelSwitch"/> 时忽略.
                                    outputTemplatePlain, //一个消息模板,述用于写入接收器的格式. 默认"{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}".
                                    null, //提供区域性特定的格式信息,或为null,
                                    8388603, //允许日志文件增长到的大约最大大小(以字节为单位).对于不受限制的增长,传递null.默认值为1 GB,为避免写入部分事件,限制内的最后一个事件即使超过限制，也将被完整写入.
                                    null, //允许在运行时更改直通最低级别的开关.
                                    false, //指示是否可以缓冲对输出文件的刷新,默认值为false.
                                    true, //允许多个进程共享日志文件,默认值为false.
                                    null, //如果提供,将以指定的时间间隔定期执行完整的磁盘刷新.
                                    RollingInterval.Day, //日志记录将滚动到新文件的时间间隔.
                                    true, //如果<code>true</code>,则在达到文件大小限制时将创建一个新文件.文件名将以<code>_NNN</code>的格式附加一个数字,第一个文件名没有编号.
                                    null, //将保留的最大日志文件数,包括当前日志文件.对于无限保留,传递null.默认值为31.
                                    Encoding.UTF8, //用于写入文本文件的字符编码.默认为UTF-8,不带BOM表.
                                    null, //(可选)启用挂接日志文件生命周期事件.
                                    null//间隔结束后,滚动日志文件将被保留的最长时间.必须大于或等于<see cref="TimeSpan.Zero"/>.如果<paramref see="rollingInterval"/>为<see cref="RollingInterval.Infinite"/>则忽略.默认值是无限期保留文件.
                                )
                           )
                    )
                    .WriteTo.Logger(cfg =>
                        cfg.Filter.ByIncludingOnly(m => m.Level > LogEventLevel.Information)
                            .WriteTo.Async(c =>
                                c.File(pathFormat, //文件的路径.
                                    LogEventLevel.Warning, //通过接收器的事件的最低级别,指定 <paramref name="levelSwitch"/> 时忽略.
                                    outputTemplateDetail, //一个消息模板,述用于写入接收器的格式. 默认"{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}".
                                    null, //提供区域性特定的格式信息,或为null,
                                    8388603, //允许日志文件增长到的大约最大大小(以字节为单位).对于不受限制的增长,传递null.默认值为1 GB,为避免写入部分事件,限制内的最后一个事件即使超过限制，也将被完整写入.
                                    null, //允许在运行时更改直通最低级别的开关.
                                    false, //指示是否可以缓冲对输出文件的刷新,默认值为false.
                                    true, //允许多个进程共享日志文件,默认值为false.
                                    null, //如果提供,将以指定的时间间隔定期执行完整的磁盘刷新.
                                    RollingInterval.Day, //日志记录将滚动到新文件的时间间隔.
                                    true, //如果<code>true</code>,则在达到文件大小限制时将创建一个新文件.文件名将以<code>_NNN</code>的格式附加一个数字,第一个文件名没有编号.
                                    null, //将保留的最大日志文件数,包括当前日志文件.对于无限保留,传递null.默认值为31.
                                    Encoding.UTF8, //用于写入文本文件的字符编码.默认为UTF-8,不带BOM表.
                                    null, //(可选)启用挂接日志文件生命周期事件.
                                    null//间隔结束后,滚动日志文件将被保留的最长时间.必须大于或等于<see cref="TimeSpan.Zero"/>.如果<paramref see="rollingInterval"/>为<see cref="RollingInterval.Infinite"/>则忽略.默认值是无限期保留文件.
                                )
                            )
                    );
            });
        }
    }
}
