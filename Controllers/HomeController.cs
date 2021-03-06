﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using wedding_planner.Models;

using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;

namespace wedding_planner.Controllers
{
    public class HomeController : Controller
    {
        private MyContext dbContext;
        public HomeController(MyContext context)
        {
            dbContext = context;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost("register")]
        public IActionResult Register(User user){
           if(ModelState.IsValid){
               User exists = dbContext.users.FirstOrDefault(u => u.Email == user.Email);

               if(exists != null){
                    ModelState.AddModelError("Email", "An account with this email already exists");
                    return View("Index");
                }
                else{
                    PasswordHasher<User> Hasher = new PasswordHasher<User>();
                    user.Password = Hasher.HashPassword(user, user.Password);
                    dbContext.Add(user);
                    //    Console.WriteLine("USER ADDED============= " + user.FirstName);
                    dbContext.SaveChanges();
                    HttpContext.Session.SetInt32("UserId", user.UserId);
                    return RedirectToAction("GetAccount", new {userId = user.UserId});
                }
           }
           else{
               return View("Index");
           }
       }

        [HttpGet("login")]
        public IActionResult Login(){
           return View("Login");
        }

        [HttpPost("processLogin")]
        public IActionResult ProcessLogin(LoginUser loginUser){
            if(ModelState.IsValid){
                User userInDb = dbContext.users.FirstOrDefault(u => u.Email == loginUser.Email);
                if(userInDb == null){
                    ModelState.AddModelError("Email", "Invalid Email/Password");
                    return View("Login");
                }
                var hasher = new PasswordHasher<LoginUser>();
                var result = hasher.VerifyHashedPassword(loginUser, userInDb.Password, loginUser.Password);
                if(result == 0){
                    ModelState.AddModelError("Password", "Incorrect Password");
                }
                HttpContext.Session.SetInt32("UserId", userInDb.UserId);
                User user = dbContext.users.FirstOrDefault(u => u.UserId == userInDb.UserId);
                TempData["name"] = user.FirstName;
                return RedirectToAction("GetAccount", new {userId = user.UserId});
            }
            else{
                return View("Login");
            }
        }

        [HttpGet("logout")]
        public IActionResult Logout(){
           HttpContext.Session.Clear();
           return View("Index");
       }

       [HttpGet("dashboard")]
       public IActionResult GetAccount(){
            int? id = HttpContext.Session.GetInt32("UserId");
            User user = dbContext.users.FirstOrDefault(u => u.UserId == id);
            TempData["name"] = user.FirstName;
            TempData["id"] = user.UserId;

            var weddings = dbContext.weddings
                .Include(w => w.MyGuests)
                .ThenInclude(r => r.User)
                .ToList();
            return View("Dashboard", weddings);
       }

       [HttpGet("newWedding")]
       public IActionResult WeddingForm(){
           int? id = HttpContext.Session.GetInt32("UserId");
           User user = dbContext.users.FirstOrDefault(u => u.UserId == id);
           TempData["name"] = user.FirstName;
           TempData["UserId"] = user.UserId;
           return View("WeddingForm");
       }

       [HttpPost("createWedding")]
       public IActionResult createWedding(string wedderOne, string wedderTwo, DateTime date, string address){
           if(ModelState.IsValid){
               if(date < DateTime.Now){
                   ModelState.AddModelError("Date", "Wedding date must be in the future");
                   return View("WeddingForm");
               }
               else{
                   int? id = HttpContext.Session.GetInt32("UserId");
                    User user = dbContext.users.FirstOrDefault(u => u.UserId == id);
                    Wedding wedding = new Wedding{
                    WedderOne = wedderOne,
                    WedderTwo = wedderTwo,
                    Date = date,
                    Address = address,
                    UserId = user.UserId
                    };

                    dbContext.weddings.Add(wedding);
                    dbContext.SaveChanges();
                    return RedirectToAction("Details");
               }
           }
           else{
               return View("WeddingForm");
           }
       }

       [HttpGet("Details")]
       public IActionResult Details(){
           return View("Details");
       }

       [HttpGet("/details/{wedId}")]
       public IActionResult OneWedding(int wedId){
           var oneWedding = dbContext.weddings
            .Include(w => w.MyGuests)
            .ThenInclude(r => r.User)
            .FirstOrDefault(w => w.WeddingId == wedId);
           return View("Details", oneWedding);
       }

       [HttpGet("RSVP/{WeddingId}")]
       public IActionResult RSVP(int weddingId){
            int? id = HttpContext.Session.GetInt32("UserId");
            User user = dbContext.users.FirstOrDefault(u => u.UserId == id);
            RSVP rsvp = new RSVP{
               UserId = user.UserId,
               WeddingId = weddingId
           };
           dbContext.rsvps.Add(rsvp);
           dbContext.SaveChanges();
           return RedirectToAction("GetAccount");
       }

       [HttpGet("/deleteWedding/{WeddingId}")]
       public IActionResult DeleteWedding(int wedId){
           Console.WriteLine("Wedding to be deleted ID ========"+ wedId);
           var wedding = dbContext.weddings.FirstOrDefault(w => w.WeddingId == wedId);
            dbContext.weddings.Remove(wedding);
            dbContext.SaveChanges();
            return RedirectToAction("GetAccount");
       }

       [HttpGet("/unRSVP/{weddingId}")]
       public IActionResult UnRSVP(int weddingId){
           int? id = HttpContext.Session.GetInt32("UserId");
            User user = dbContext.users.FirstOrDefault(u => u.UserId == id);

            //look for a specific RSVP based on an userId and weddingId
            var rsvp = dbContext.rsvps.Where(r => r.UserId == user.UserId).FirstOrDefault(w => w.WeddingId == weddingId);
           if(rsvp != null){
               dbContext.rsvps.Remove(rsvp);
               dbContext.SaveChanges();
               return RedirectToAction("GetAccount");
           }
           else{
               return View("Dashboard");
           }
       }
    }
}
