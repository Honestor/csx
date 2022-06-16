using Dapper;
using Framework.BlobStoring;
using Framework.BlobStoring.FileSystem;
using Framework.Core.Data;
using Framework.Core.Dependency;
using Framework.Excel.Npoi;
using Framework.Uow;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.AspNetCore.Test.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [ApiExplorerSettings(GroupName = "测试模块1")]
    public class WeatherForecastController : ControllerBase
    {
        private IUserRepository _userRepository;
        private IExcelWriter _excelWriter;
        private IExcelReader _excelReader;
        private IBlobContainer<FileSystemBlobContainer> _fileContainer;
        private UserService _userService;
        private Context _context;
        private IUnitOfWorkManager _unitOfWorkManager;
        private IDbProvider _dbProvider;
        public WeatherForecastController(
            IUserRepository userRepository,
            IExcelWriter excelWriter,
            IExcelReader excelReader,
            IBlobContainer<FileSystemBlobContainer> fileContainer, Context context, UserService userService, IUnitOfWorkManager unitOfWorkManager, IDbProvider dbProvider)
        {
            _userRepository = userRepository;
            _excelWriter = excelWriter;
            _excelReader = excelReader;
            _fileContainer = fileContainer;
            _context = context;
            _userService = userService;
            _unitOfWorkManager = unitOfWorkManager;
            _dbProvider = dbProvider;
        }

        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        public class TestModel
        {
            /// <summary>
            /// 名称
            /// </summary>
            [Required]
            [MaxLength(10,ErrorMessage ="最大长度不能超过10")]
            public string Name { get; set; }

            public DateTime StartTime { get; set; }

            public DateTime? EndTime { get; set; }
        }

        public class Person
        {
            public string Name { get; set; } 

            public int Age { get; set; } 

            public DateTime CreateTime { get; set; }

            public DateTime? ModifyTime { get; set; }

            public bool IsMan { get; set; } 

            public PositionType Position { get; set; }

            public float Money { get; set; }

            public decimal High { get; set; }

            public double Weight { get; set; } 

            public Person Child { get; set; }

            public ChildInfo ChildInfo { get; set; }
        }

        public class ChildInfo
        {
            public string Name { get; set; }

            public int Age { get; set; }

            public DateTime CreateTime { get; set; }

            public DateTime? ModifyTime { get; set; }

        }

        /// <summary>
        /// 职位
        /// </summary>
        public enum PositionType
        {
            /// <summary>
            /// 领导
            /// </summary>
            Leader = 0,

            /// <summary>
            /// 普通员工
            /// </summary>
            Worker = 1
        }

        public class CustomerAttribute : Attribute, IModelValidator
        {
            public IEnumerable<ModelValidationResult> Validate(ModelValidationContext context)
            {
                return new List<ModelValidationResult>() { new ModelValidationResult("Name", "不能为空") };
            }
        }

        /// <summary>
        /// 测试模块一  方法
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("test1")]
        public async Task<List<WorkRecord>> TestValidate(Person model)
        {
           await Task.CompletedTask;
            var wList = new List<WorkRecord>()
            {
                new WorkRecord(){ WorkTime=DateTime.Now,Content="aaaaaaaaaaaaaaaaaaaaaaaaaaaa" },
                new WorkRecord(){ WorkTime=DateTime.Now,Content="bbbbbbbbbbbbbbbbbbbbbbbbb" },
                new WorkRecord(){ WorkTime=DateTime.Now,Content="ccccccccccccccccccccccccccccccc" },
                new WorkRecord(){ WorkTime=DateTime.Now,Content="dddddddddddddddddddddddd" },
            };
            return wList;
        }

        #region oracle异步测试
        [HttpGet]
        public async Task Get()
        {
            OracleConfiguration.s
           var model = new TheadModel();
            model.one = Thread.CurrentThread.ManagedThreadId.ToString();
            var connection = new OracleConnection("User Id=wpsm;Password=wpsm;Data Source=172.18.100.231:1521/orcl;");
            model.two = Thread.CurrentThread.ManagedThreadId.ToString();
            await connection.OpenAsync();
            model.three = Thread.CurrentThread.ManagedThreadId.ToString();
            var result = await connection.QueryAsync<OracleTest>("select * from HeartRateIndex");
            dic.TryAdd(Guid.NewGuid().ToString(), model);
        }
        #endregion


        private static ConcurrentDictionary<string, TheadModel> dic = new ConcurrentDictionary<string, TheadModel>();
        #region Mysql异步测试
        //[HttpGet]
        //public async Task Get()
        //{
        //    using (var uow = _unitOfWorkManager.Begin(new UnitOfWorkOptions() { }))
        //    {
        //        var model = new TheadModel();
        //        model.one = Thread.CurrentThread.ManagedThreadId.ToString();
        //        var conn = await _dbProvider.GetConnectionAsync();
        //        model.two = Thread.CurrentThread.ManagedThreadId.ToString();
        //        var result = await conn.QueryAsync<ac_user_copy>("select * from ac_user_copy");
        //        model.three = Thread.CurrentThread.ManagedThreadId.ToString();
        //        dic.TryAdd(Guid.NewGuid().ToString(), model);
        //    }
        //} 
        #endregion

        public class TheadModel
        { 
            public string one { get; set; }

            public string two { get; set; }

            public string three { get; set; }
        }

        public class ac_user_copy
        {

            public string name { get; set; }
        }
        

        public class OracleTest
        {
            public int Id { get; set; }

            public string RoomNumber { get; set; }
        }

        [HttpGet]
        [Route("excelwrite")]
        public void Get1()
        {
            var list = new List<Person>()
            {
                new Person(){ Name="张三",Age=22 },
                new Person(){ Name="李四",Age=23 },
                new Person(){ Name="王五",Age=24 },
                new Person(){ Name="赵柳",Age=25 },
            };
            _excelWriter.Write(list, $"{AppDomain.CurrentDomain.BaseDirectory}\\excel\\output\\{Guid.NewGuid().ToString()}.xlsx");

            var wList = new List<WorkRecord>()
            {
                new WorkRecord(){ WorkTime=DateTime.Now,Content="aaaaaaaaaaaaaaaaaaaaaaaaaaaa" },
                new WorkRecord(){ WorkTime=DateTime.Now,Content="aaaaaaaaaaaaaaaaaaaaaaaaaaaa" },
                new WorkRecord(){ WorkTime=DateTime.Now,Content="aaaaaaaaaaaaaaaaaaaaaaaaaaaa" },
                new WorkRecord(){ WorkTime=DateTime.Now,Content="aaaaaaaaaaaaaaaaaaaaaaaaaaaa" },
            };
            _excelWriter.Write(wList, $"{AppDomain.CurrentDomain.BaseDirectory}\\excel\\output\\{Guid.NewGuid().ToString()}.xlsx");
        }

        [HttpGet]
        [Route("excelwritemany")]
        public void Get2()
        {
            var list = new List<Person>()
            {
                new Person(){ Name="张三",Age=22 },
                new Person(){ Name="李四",Age=23 },
                new Person(){ Name="王五",Age=24 },
                new Person(){ Name="赵柳",Age=25 },
            };

            var wList = new List<WorkRecord>()
            {
                new WorkRecord(){ WorkTime=DateTime.Now,Content="aaaaaaaaaaaaaaaaaaaaaaaaaaaa" },
                new WorkRecord(){ WorkTime=DateTime.Now,Content="bbbbbbbbbbbbbbbbbbbbbbbbb" },
                new WorkRecord(){ WorkTime=DateTime.Now,Content="ccccccccccccccccccccccccccccccc" },
                new WorkRecord(){ WorkTime=DateTime.Now,Content="dddddddddddddddddddddddd" },
            };
            _excelWriter
                .Write(list)
                .Write(wList)
                .SaveAs($"{AppDomain.CurrentDomain.BaseDirectory}\\excel\\output\\{Guid.NewGuid().ToString()}.xlsx");
        }

        [HttpGet]
        [Route("excelread")]
        public void Get3()
        {
           
        }
    }


    public class Context:IScoped,IDisposable
    { 
        public string Data { get; set; }

        public void Dispose()
        {
           
        }
    }

    public class UserService : IScoped
    {
        private Context _context;
        public UserService(Context context)
        {
            _context = context;
        }

        public void Get()
        {
            var context = _context;

        }
    }
}
