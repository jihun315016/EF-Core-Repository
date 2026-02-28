using EmployeeAPP.Models.Contexts;
using EmployeeAPP.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EmployeeAPP.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TestController(AppDbContext context)
        {
            _context = context;

            //// 실습용 데이터가 없을 경우를 대비한 세팅 (생성자에서 간편하게 처리)
            //if (!_context.Departments.Any())
            //{
            //    var dept1 = new Department { Name = "개발팀" };
            //    dept1.Employees.Add(new Employee { Name = "김철수" });
            //    dept1.Employees.Add(new Employee { Name = "이영희" });

            //    var dept2 = new Department { Name = "인사팀" };
            //    dept2.Employees.Add(new Employee { Name = "박지민" });

            //    _context.Departments.AddRange(dept1, dept2);
            //    _context.SaveChanges();
            //}
        }

        /// <summary>
        /// 시나리오 1: N+1 문제 발생 (지연 로딩 효과 재현)
        /// Include()를 명시적으로 사용하는 경우 JOIN을 통해 데이터를 한 번에 조회(즉시 로딩)
        /// 처리 시간 : 3.69s
        /// </summary>
        [HttpGet("n-plus-one")]
        public IActionResult GetNPlusOne()
        {
            // 1. 부서 목록만 가져옴 (SELECT * FROM Departments 쿼리 1번 실행)
            var departments = _context.Departments.ToList();

            // 2. JSON 직렬화 과정에서 각 부서의 'Employees' 리스트에 접근하게 됨.
            // 이때 EF Core는 각 부서마다 추가 쿼리를 날림 (부서가 N개면 N번 추가 실행)
            return Ok(departments);
        }

        /// <summary>
        /// 시나리오 2: 즉시 로딩 (Eager Loading)으로 최적화
        /// 처리 시간 : 195s
        /// Select 안에 자식 데이터를 넣으면, 지연 로딩도 즉시 로딩도 아님
        /// 그냥 LINQ에 맞는 하나의 쿼리로 변환하여 가져오는 것
        /// </summary>
        [HttpGet("eager")]
        public IActionResult GetEager()
        {
            // Include를 사용하여 JOIN으로 한 번에 모든 데이터를 가져옴 (쿼리 딱 1번 실행)
            // AsNoTracking은 조회 성능을 극대화하기 위해 변경 추적 기능을 끔
            var departments = _context.Departments
                                      .Include(d => d.Employees)
                                      .AsNoTracking()
                                      .ToList();

            return Ok(departments);
        }

        /// <summary>
        /// 시나리오 3: 필요한 데이터만 추출 (Projection)
        /// </summary>
        [HttpGet("projection")]
        public IActionResult GetProjection()
        {
            // 모든 컬럼(*)이 아닌, 필요한 데이터만 뽑아서 전송
            // 무거운 Entity 객체 대신 가벼운 익명 객체(또는 DTO)로 변환
            var result = _context.Departments
                .Select(d => new
                {
                    DeptName = d.Name,
                    TotalEmployees = d.Employees.Count, // 내부적으로 SQL COUNT()로 변환됨
                    EmpNames = d.Employees.Select(e => e.Name).ToList()
                })
                .AsNoTracking()
                .ToList();

            return Ok(result);
        }

        /// <summary>
        /// 시나리오 4: IQueryable vs IEnumerable (메모리 로드 시점 차이)
        /// </summary>
        [HttpGet("query-vs-enumerable")]
        public IActionResult GetQueryTest(string name)
        {
            // 1. IQueryable: DB에서 필터링해서 결과만 가져옴 (권장)
            // 로그 확인 시: SELECT ... WHERE Name = 'name'
            var query = _context.Employees.Where(e => e.Name == name).ToList();

            // 2. IEnumerable: 전체 데이터를 메모리에 올린 후 C#에서 필터링 (위험)
            // 로그 확인 시: SELECT * FROM Employees (조건절 없음)
            var list = _context.Employees.AsEnumerable().Where(e => e.Name == name).ToList();

            return Ok(new { queryResult = query, listResult = list });
        }
    }
}
