﻿using BanSach.Components.Data;
using BanSach.Components.IService;
using BanSach.Components.Model;
using Microsoft.EntityFrameworkCore;

namespace BanSach.Components.Services
{
    public class CartService : ICartService
    {
        private readonly AppDbContext db;
        private readonly HttpClient http;

        public CartService(AppDbContext _db, HttpClient _http)
        {
            db = _db;
            http = _http;
        }

        public async Task<List<Product_cart>> GetAllPCart()
        {
            return await db.Product_carts.Include(p => p.Product).ToListAsync();
        }
        public async Task<List<Product_cart>> GetPCartUser(int UserID)
        {
            return await db.Product_carts.Where(x => x.UserId == UserID && x.IsCheckedOut == false).Include(p => p.Product).ToListAsync();
        }

        public async Task<Product_cart> CreatePCart(Product_cart productCart)
        {
            var existingProductCart = await db.Product_carts
                .FirstOrDefaultAsync(pc => pc.UserId == productCart.UserId && pc.ProductId == productCart.ProductId && pc.IsCheckedOut == false);

            if (existingProductCart != null)
            {
                existingProductCart.Quantity += productCart.Quantity;
                existingProductCart.Updated = DateTime.Now;
            }
            else
            {
                productCart.Created = DateTime.Now;
                productCart.Updated = DateTime.Now;
                db.Product_carts.Add(productCart);
            }

            await db.SaveChangesAsync();
            return productCart;
        }

        public async Task DeletePCart(Product_cart productCart)
        {
            var cartItem = await db.Product_carts
                                    .FirstOrDefaultAsync(pc => pc.UserId == productCart.UserId && pc.ProductId == productCart.ProductId);

            if (cartItem != null)
            {
                db.Product_carts.Remove(cartItem);
                await db.SaveChangesAsync();
            }
        }

        public async Task<(string message, string code, int billID)> PlaceProductBill(List<Product_bill> productBill, int userId, decimal totalPrice)
        {
            var address = await db.Address.FirstOrDefaultAsync(x => x.UserId == userId);
            if (address == null)
            {
                return ("Không tìm thấy địa chỉ của người dùng.", "404", 0);
            }

            Bill newBill = new Bill();
            newBill.UserID = userId;
            newBill.AddressId = address.AddressId;
            newBill.DeliveryId = db.Deliveries.Select(x => x.DeliveryId).FirstOrDefault();
            newBill.PayStatus = "Đang chờ";
            newBill.TotalPrice = totalPrice;
            newBill.Note = "Đang chờ duyệt";
            newBill.Created_at = DateTime.Now;
            newBill.Updated_at = DateTime.Now;
            newBill.Status = OrderStatus.Pending.ToString();
            newBill.ApproveBill = false;
            await db.AddAsync(newBill);
            await db.SaveChangesAsync();

            foreach (var cartItem in productBill)
            {
                var productPrice = await db.Products.Where(x => x.ProductId == cartItem.ProductId).FirstOrDefaultAsync();
                if (productPrice != null)
                {
                    Product_bill newDetail = new Product_bill();
                    newDetail.ProductId = cartItem.ProductId;
                    newDetail.BillId = newBill.BillId;
                    newDetail.Price = productPrice.SellPrice;
                    newDetail.Quantity = cartItem.Quantity;
                    newDetail.Created = DateTime.Now;
                    newDetail.Updated = DateTime.Now;
                    await db.AddAsync(newDetail);
                }

            }
            await db.SaveChangesAsync();
            return ("Thêm đơn hàng thành công", "200", newBill.BillId);
        }

        public async Task<Product_bill> GetProductBillById(int productBillId)
        {
            return await db.Product_bills
                    .FirstOrDefaultAsync(pb => pb.ProductBillId == productBillId);
        }

        public async Task<PaymentResult> ProcessPayment(int productBillId, string paymentMethod)
        {
            try
            {
                var productBill = await db.Product_bills.FirstOrDefaultAsync(pb => pb.ProductBillId == productBillId);

                //if (productBill == null)
                //{
                //    return new PaymentResult { IsSuccess = false, Message = "Hóa đơn không tồn tại." };
                //}

                //if (paymentMethod == "Tiền mặt")
                //{
                //    productBill.PaymentStatus = "Đã đặt đơn và xác nhận thanh toán bằng tiền mặt khi nhận hàng.";
                //}
                //else if (paymentMethod == "Ví điện tử")
                //{
                //    productBill.PaymentStatus = "Đã đặt đơn và chờ thanh toán qua ví điện tử.";
                //}
                //else
                //{
                //    return new PaymentResult { IsSuccess = false, Message = "Phương thức thanh toán không hợp lệ." };
                //}

                await db.SaveChangesAsync();
                return new PaymentResult { IsSuccess = true, Message = "Thanh toán thành công." };
            }
            catch (Exception ex)
            {
                return new PaymentResult { IsSuccess = false, Message = $"Lỗi trong quá trình thanh toán: {ex.Message}" };
            }
        }

        public async Task<int> SaveShippingAddress(Address address)
        {
            db.Address.Add(address);
            await db.SaveChangesAsync();
            return address.AddressId;
        }

        public async Task<ServiceResponse<int>> CreateBill(int addressId, int userId, decimal totalPrice, string note)
        {
            var response = new ServiceResponse<int>();
            try
            {
                var bill = new Bill
                {
                    AddressId = addressId,
                    TotalPrice = (int)totalPrice,
                    Note = note,
                    Status = "Đang chờ",
                    Created_at = DateTime.Now
                };

                db.Bill.Add(bill);
                await db.SaveChangesAsync();
                response.Data = bill.BillId;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Lỗi khi tạo hóa đơn: {ex.Message}";
            }
            return response;
        }

        public async Task<ServiceResponse<bool>> LinkBillToProductBill(int billId, List<Product_bill> productBills)
        {
            var response = new ServiceResponse<bool>();
            try
            {
                foreach (var productBill in productBills)
                {
                    productBill.BillId = billId;
                    productBill.Updated = DateTime.Now;
                    db.Product_bills.Update(productBill);
                }

                await db.SaveChangesAsync();
                response.Data = true;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Lỗi khi liên kết hóa đơn: {ex.Message}";
            }
            return response;
        }

        public async Task SaveAddress(Address address)
        {
            db.Add(address);
            await db.SaveChangesAsync();
        }

        public async Task<bool> UpdateBillAsync(Bill bill)
        {
            try
            {
                await db.Bill.AddAsync(bill);
                await db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("Không thể cập nhật hóa đơn: " + ex.Message);
            }
        }

        public async Task<(Bill, int code, string message)> CreateBillMain(List<Product_cart> cart)
        {
            var userId = cart.First().UserId;

            decimal totalPrice = cart.Sum(cartItem => cartItem.Price * cartItem.Quantity);

            var checkBillCompled = await db.Bill.FirstOrDefaultAsync(x => x.UserID == userId && x.ApproveBill == false && x.Status == "Pending");
            if (checkBillCompled == null)
            {
                Bill newBill = new Bill
                {
                    UserID = userId,
                    AddressId = 0,
                    DeliveryId = 0,
                    PayStatus = "Đang chờ",
                    TotalPrice = totalPrice,
                    Note = "Đơn đang thanh toán",
                    Created_at = DateTime.UtcNow,
                    Updated_at = DateTime.UtcNow,
                    Status = OrderStatus.Pending.ToString(),
                    ApproveBill = false
                };
                await db.Bill.AddAsync(newBill);
                await db.SaveChangesAsync();
                var productBills = cart.Select(cartItem => new Product_bill
                {
                    BillId = newBill.BillId,
                    Price = cartItem.Price,
                    ProductId = cartItem.ProductId,
                    Quantity = cartItem.Quantity,
                    Created = DateTime.Now,
                    Updated = DateTime.Now
                }).ToList();

                await db.AddRangeAsync(productBills);
                db.Product_carts.RemoveRange(cart);
                await db.SaveChangesAsync();
                return (newBill, 200, "Đã lên hóa đơn thành công");
            }
            else
            {
                checkBillCompled.TotalPrice += totalPrice;
                foreach (var cartItem in cart)
                {
                    var existingProductBill = db.Product_bills
                        .FirstOrDefault(pb => pb.BillId == checkBillCompled.BillId && pb.ProductId == cartItem.ProductId);

                    if (existingProductBill != null)
                    {
                        existingProductBill.Quantity += cartItem.Quantity;
                        existingProductBill.Updated = DateTime.Now;
                    }
                    else
                    {
                        var newProductBill = new Product_bill
                        {
                            BillId = checkBillCompled.BillId,
                            Price = cartItem.Price,
                            ProductId = cartItem.ProductId,
                            Quantity = cartItem.Quantity,
                            Created = DateTime.Now,
                            Updated = DateTime.Now
                        };
                        await db.AddAsync(newProductBill);

                    }
                }

                db.Product_carts.RemoveRange(cart);
                await db.SaveChangesAsync();
                return (checkBillCompled, 200, "Đã lên hóa đơn thành công");
            }


        }
        public async Task<(Bill, int code, string message)> CreateBillNow(Product product, int userID, int quantity)
        {

            decimal totalPrice = product.SellPrice * quantity;

            var checkBillCompled = await db.Bill.FirstOrDefaultAsync(x => x.UserID == userID && x.ApproveBill == false && x.Status == "Pending");
            if (checkBillCompled == null)
            {
                Bill newBill = new Bill
                {
                    UserID = userID,
                    AddressId = 0,
                    DeliveryId = 0,
                    PayStatus = "Đang chờ",
                    TotalPrice = totalPrice,
                    Note = "Đơn đang thanh toán",
                    Created_at = DateTime.UtcNow,
                    Updated_at = DateTime.UtcNow,
                    Status = OrderStatus.Pending.ToString(),
                    ApproveBill = false
                };
                await db.Bill.AddAsync(newBill);
                await db.SaveChangesAsync();
                var productBills = new Product_bill
                {
                    BillId = newBill.BillId,
                    Price = product.SellPrice,
                    ProductId = product.ProductId,
                    Quantity = quantity,
                    Created = DateTime.Now,
                    Updated = DateTime.Now
                };

                await db.AddAsync(productBills);
                await db.SaveChangesAsync();
                return (newBill, 200, "Đã lên hóa đơn thành công");
            }
            else
            {
                checkBillCompled.TotalPrice += totalPrice;
                var existingProductBill = db.Product_bills
                     .FirstOrDefault(pb => pb.BillId == checkBillCompled.BillId && pb.ProductId == product.ProductId);

                if (existingProductBill != null)
                {
                    existingProductBill.Quantity += quantity;
                    existingProductBill.Updated = DateTime.Now;
                }
                else
                {
                    var newProductBill = new Product_bill
                    {
                        BillId = checkBillCompled.BillId,
                        Price = product.SellPrice,
                        ProductId = product.ProductId,
                        Quantity = quantity,
                        Created = DateTime.Now,
                        Updated = DateTime.Now
                    };
                    await db.AddAsync(newProductBill);

                }
                await db.SaveChangesAsync();
                return (checkBillCompled, 200, "Đã lên hóa đơn thành công");
            }


        }

    }
}
