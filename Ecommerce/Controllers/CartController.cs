using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Ecommerce.Models;
using Ecommerce.Helpers;
using System.Web.Script.Serialization;

namespace Ecommerce.Controllers
{
    public class CartController : Controller
    {
        private ProductDBContext db = new ProductDBContext();

        // GET: Cart
        public ActionResult Index()
        {
            GlobalMethods.MaybeInitializeSession();
            
            /* Query all products by list of cart item IDs stored in session. */
            var cart_items = db.Products.Where(item => SessionSingleton.Current.Cart.Keys.Contains(item.id));

            ViewBag.Session = SessionSingleton.Current.Cart;

            return View(cart_items.ToList());
        }

        // GET: Cart
        [HttpGet]
        public ActionResult Checkout()
        {
            GlobalMethods.MaybeInitializeSession();

            /* Query all products by list of cart item IDs store in session. */
            var cart_items = db.Products.Where(item => SessionSingleton.Current.Cart.Keys.Contains(item.id));

            ViewBag.Session = SessionSingleton.Current.Cart;

            return View(cart_items.ToList());
        }
        
        [HttpPost]
        public ActionResult Checkout(FormCollection collection)
        {
            

            return RedirectToAction("Checkout", "Cart");
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult Actions(FormCollection collection)
        {
            GlobalMethods.MaybeInitializeSession();

            string msg = "";
            int status = 0;
            int item_id = Convert.ToInt32(collection["item_id"]);
            var item = db.Products.First(i => i.id == item_id);
            var session = SessionSingleton.Current.Cart;

            if(item != null) { 
                /* Add Item to Cart */
                if (collection["action"] == "add-item-to-cart")
                {
                    if(session.ContainsKey(item_id))
                    {
                        msg = "Item already exist in Cart";
                    }
                    else if (string.IsNullOrEmpty(collection["quantity"]))
                    {
                        msg = "Quantity must be greater than zero";
                    }
                    else
                    {
                        string json = new JavaScriptSerializer().Serialize(new
                        {
                            quantity = collection["quantity"],
                            date_added = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")
                        });
                        session.Add(item_id, json);
                        msg = "Item successfully added to cart";
                        status = 1;
                    }
                }

                /* Remove Item from Cart */
                if (collection["action"] == "remove-item-from-cart")
                {
                    if (session.ContainsKey(item_id))
                    {
                        session.Remove(item_id);
                        msg = "Item successfully removed from cart";
                        status = 1;
                    }
                    else
                    {
                        msg = "Item does not exist in cart";
                    }
                }

                /* Update Item Quantity in Cart */
                if (collection["action"] == "update-item-quantity-in-cart")
                {
                    if (!session.ContainsKey(item_id))
                    {
                        msg = "Item does not exist in cart";
                    }
                    else if (string.IsNullOrEmpty(collection["quantity"]))
                    {
                        msg = "Quantity must be greater than zero";
                    }
                    else if (Convert.ToInt32(collection["quantity"]) > item.quantity)
                    {
                        msg = "Product: " + item.name + " only have '" + item.quantity + "' items left in stock";
                    }
                    else
                    {
                        string json = new JavaScriptSerializer().Serialize(new
                        {
                            quantity = collection["quantity"],
                            date_added = DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss")
                        });

                        session[item_id] = json;
                        msg = "Quantity successfully updated";
                        status = 1;
                    }
                }
            }
            else
            {
                msg = "The item you are trying to process is invalid";
            }

            dynamic query_string = new { status = status, msg = HttpUtility.UrlEncode(msg) };

            if (collection["redirect_page"] != null)
            {
                if (collection["redirect_page"] == "Cart")
                {
                    return RedirectToAction("Index", "Cart", query_string);
                }
                else if (collection["redirect_page"] == "Single")
                {
                    return RedirectToAction("Details/" + item_id, "Products", query_string);
                }
            }
            
            return RedirectToAction("Index", "Products", query_string);
        }

    }
}
