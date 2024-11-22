﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HieuEMart.Areas.Admin.Controllers
{
	[Area("Admin")]
    [Authorize(Roles = "Admin, Staff")]
    public class HomeController : Controller
	{
		public IActionResult Index()
		{
			return View();
		}
	}
}
