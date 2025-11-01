using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PreschoolEnrollmentSystem.Admin.Models.ViewModels.Parent
{
	public class ParentDetailsViewModel : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}
