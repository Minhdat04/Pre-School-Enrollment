using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreschoolEnrollmentSystem.API.Controllers
{
	public class ClassesControllers : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}
