﻿using BanSach.Components.Model;
using BanSach.Components.Model.ViewModel;

namespace BanSach.Components.IService
{
    public interface IBillService
    {
        Task<List<Product_bill>> GetAllProduct_bill();
        Task DeleteProduct_bill(Product_bill Product_bill);
        Task EditProduct_bill(Product_bill Product_bill);

        Task<List<Bill>> GetAllbill();
        Task<(decimal doanhThu, int doanhSo, int soDon)> GetDoanhThuThangHienTai(DateTime currentDate);

        Task Deletbill(Bill bill);
        Task Editbill(Bill bill);
        Task<Delivery?> GetDeById(int DeId);
        Task<List<Delivery>> GetAllDelivery();


        Task<bool> CompleteProductBillAsync(BillVanDonDTO bill);
        Task<List<DoanhThuViewModel>> GetAllBillDoanhThu(DateTime? fromDate, DateTime? toDate);
        Task<PagedResult<BillVanDonDTO>> GetBillDetailsAsync(int page, int pageSize);

        Task<List<TopProductViewModel>> GetTopProducts(DateTime? fromDate, DateTime? toDate);
        Task<bool> ApproveProductBillAsync(BillVanDonDTO bill);
        Task<bool> DeleteProductBillAsync(BillVanDonDTO bill);
        Task<List<ProductBillDetailDto>> GetProductsByBillId(int billId);
        Task<Address> GetAddressUser(int UserID);
        Task<(Delivery, int code, string message)> SetDeliveryBill(int? BillID, int DeliveryID, int WarehouseID);

        Task<(Bill, int code, string message)> ConfirmOrder(Bill model, string type, ShippingInfoDTO address);

        Task<List<Warehouse>> GetAllWarehouse();


    }
}
