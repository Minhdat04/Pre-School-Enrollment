using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreschoolEnrollmentSystem.Admin.Models.ViewModels.Enrollment
{
	public class EnrollmentHistoryViewModel : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}
