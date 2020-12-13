using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BankAccounts.Models;
using Microsoft.AspNetCore.Identity;
using BankAccounts.Context;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BankAccounts.Controllers
{
    public class HomeController : Controller
    {
        private MyContext _context;

        public decimal AcctTotal()
        {
            decimal AcctTotal = 0;
            foreach (var v in ViewBag.User.TransactionList)
            {
                AcctTotal += v.Amount;
            }
            return AcctTotal;
        }

        public User LoggedInUser()
        {
            int? LoggedID = HttpContext.Session.GetInt32("LoggedInUser");
            User logged = _context.Users.FirstOrDefault(u => u.UserId == LoggedID);
            return logged;
        }

        public int UserID()
        {
            int UserID = LoggedInUser().UserId;
            return UserID;
        }
        public HomeController(MyContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("register")]
        public IActionResult Register(User reg)
        {
            //These two lines will hash our password for us.
            if (ModelState.IsValid)
            {
                if (_context.Users.Any(u => u.Email == reg.Email))
                {
                    ModelState.AddModelError("Email", "Email already in use!");
                    return View("Index");
                }
                else
                {
                    PasswordHasher<User> Hasher = new PasswordHasher<User>();
                    reg.Password = Hasher.HashPassword(reg, reg.Password);
                    _context.Users.Add(reg);
                    _context.SaveChanges();
                    var userInDb = _context.Users.FirstOrDefault(u => u.Email == reg.Email);
                    HttpContext.Session.SetInt32("LoggedInUser", userInDb.UserId);
                    return Redirect($"account/{UserID()}");
                }
            }
            else
            {
                return View("Index");
            }
        }
        [HttpGet("login")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost("LoginMethod")]
        public IActionResult LoginMethod(LoginUser log)
        {

            if (ModelState.IsValid)
            {
                // If inital ModelState is valid, query for a user with provided email
                var userInDb = _context.Users.FirstOrDefault(u => u.Email == log.LoginEmail);
                // If no user exists with provided email
                if (userInDb == null)
                {
                    // Add an error to ModelState and return to View!
                    ModelState.AddModelError("Email", "Invalid Email/Password");
                    return View("Login");
                }

                // Initialize hasher object
                var hasher = new PasswordHasher<LoginUser>();

                // verify provided password against hash stored in db
                var result = hasher.VerifyHashedPassword(log, userInDb.Password, log.LoginPassword);

                // result can be compared to 0 for failure
                if (result == 0)
                {
                    ModelState.AddModelError("Email/Password", "Invalid Email/Password");
                    return View("Login");
                    // handle failure (this should be similar to how "existing email" is handled)
                }
                else
                {
                    HttpContext.Session.SetInt32("LoggedInUser", userInDb.UserId);
                    return Redirect($"account/{UserID()}");
                }
            }
            else
            {
                return View("Index");
            }
        }
        [HttpGet("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
        [HttpGet("account/{UserID}")]
        public IActionResult Account(int UserID)
        {
            if (LoggedInUser() == null)
                return RedirectToAction("Index");

            int ID = LoggedInUser().UserId;
            if (UserID != ID)
            {
                Console.WriteLine("You're not this user");
                return RedirectToAction("Logout");
            }
            else
            {
                ViewBag.User = _context.Users
                .Include(u => u.TransactionList).OrderBy(u => u.CreatedAt)
                .FirstOrDefault(u => u.UserId == UserID);
                ViewBag.Total = AcctTotal();
                return View();
            }
        }

        [HttpPost("new")]
        public IActionResult NewTransaction(Transaction newT)
        {
            if (ModelState.IsValid)
            {
                newT.UserId = UserID();
                ViewBag.User = _context.Users
                .Include(u => u.TransactionList).OrderBy(u => u.CreatedAt)
                .FirstOrDefault(u => u.UserId == newT.UserId);
                decimal CurrentTotal = AcctTotal();
                decimal check = CurrentTotal += newT.Amount;
                if (check < 0)
                {
                    return Redirect($"account/{UserID()}");

                }
                else
                {
                    _context.Transactions.Add(newT);
                    _context.SaveChanges();
                    return Redirect($"account/{UserID()}");
                }

            }
            else
            {
                return View("Account", UserID());
            }
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
