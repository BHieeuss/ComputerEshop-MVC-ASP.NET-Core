﻿using HieuEMart.Models;
using HieuEMart.Repository;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Numerics;

namespace HieuEMart.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/User")]
    public class UserController : Controller
    {
        private readonly UserManager<AppUserModel> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly DataContext _dataContext;

        public UserController(UserManager<AppUserModel> userManager, RoleManager<IdentityRole> roleManager, DataContext dataContext)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _dataContext = dataContext;

        }
        [HttpGet]
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            //Liên kết data 3 bảng
            var usersWithRoles = await (from u in _dataContext.Users
                                        join ur in _dataContext.UserRoles on u.Id equals ur.UserId into userRoles
                                        from ur in userRoles.DefaultIfEmpty()
                                        join r in _dataContext.Roles on ur.RoleId equals r.Id into roles
                                        from r in roles.DefaultIfEmpty()
                                        select new { User = u, RoleName = r != null ? r.Name : "Chưa phân quyền" })
                             .ToListAsync();
            return View(usersWithRoles);
        }

        [HttpGet]
        [Route("Create")]
        public async Task<IActionResult> Create()
        {
            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Id", "Name");
            return View(new AppUserModel());
        }

        [HttpGet]
        [Route("Edit")]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Id", "Name");

            return View(user);
        }

        [HttpPost]
        [Route("Edit")]
        public async Task<IActionResult> Edit(string id, AppUserModel user)
        {
            var existingUser = await _userManager.FindByIdAsync(id);
            if (existingUser == null)
            {
                return NotFound();
            }
            if (ModelState.IsValid)
            {
                existingUser.UserName = user.UserName;
                existingUser.Email = user.Email;
                existingUser.PhoneNumber = user.PhoneNumber;
                existingUser.RoleId = user.RoleId;
            }
            var updateUserResult = await _userManager.UpdateAsync(existingUser);
            if (updateUserResult.Succeeded)
            {
                return RedirectToAction("Index", "User");
            }
            else
            {
                AddIdentityErrors(updateUserResult);
                return View(existingUser);
            }
            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Id", "Name");

            TempData["error"] = "Không cập nhật thành công";
            var errors = ModelState.Values.SelectMany(x => x.Errors.Select(e => e.ErrorMessage)).ToList();
            string errorMessage = string.Join("\n", errors);

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Create")]
        public async Task<IActionResult> Create(AppUserModel user)
        {
            if (ModelState.IsValid)
            {
                // Check if the username already exists
                var existingUser = await _userManager.FindByNameAsync(user.UserName);
                if (existingUser != null)
                {
                    ModelState.AddModelError("UserName", "Tên người dùng đã tồn tại.");
                    TempData["error"] = "Tên người dùng đã tồn tại!";
                    return View(user);
                }

                var createUserResult = await _userManager.CreateAsync(user, user.PasswordHash);
                if (createUserResult.Succeeded)
                {
                    var createUser = await _userManager.Users.Where(u => u.Email == user.Email).FirstOrDefaultAsync();
                    var userId = createUser.Id;
                    var role = await _roleManager.FindByIdAsync(user.RoleId);

                    if (role != null)
                    {
                        var addToRoleResult = await _userManager.AddToRoleAsync(createUser, role.Name);
                        if (!addToRoleResult.Succeeded)
                        {
                            AddIdentityErrors(createUserResult);
                            return View(user);
                        }
                    }
                    else
                    {
                        ModelState.AddModelError("Role", "Vai trò không hợp lệ.");
                        return View(user);
                    }
                    return RedirectToAction("Index", "User");
                }
                else
                {
                    AddIdentityErrors(createUserResult);
                    return View(user);
                }
            }
            else
            {
                TempData["error"] = "Vui lòng kiểm tra lại dữ liệu!";
                List<string> errors = new List<string>();
                foreach (var value in ModelState.Values)
                {
                    foreach (var error in value.Errors)
                    {
                        errors.Add(error.ErrorMessage);
                    }
                }
                string errorMessage = string.Join("\n", errors);
                return BadRequest(errorMessage);
            }

            var roles = await _roleManager.Roles.ToListAsync();
            ViewBag.Roles = new SelectList(roles, "Id", "Name");
            return View(user);
        }


        [HttpGet]
        [Route("Delete")]
        public async Task<IActionResult> Delete(string id)
        {
            if(string.IsNullOrEmpty(id))
            {
                return NotFound();
            }
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            var deleteResult = await _userManager.DeleteAsync(user);
            if (!deleteResult.Succeeded)
            {
                return View("Error");
            }
            TempData["Success"] = "Xóa Người dùng thành công!";
            return RedirectToAction("Index");
        }

        private void AddIdentityErrors(IdentityResult identityResult)
        {
            foreach(var error in identityResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}
