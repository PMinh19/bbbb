using BanSach.Components.Data;
using BanSach.Components.Model;
using Microsoft.EntityFrameworkCore;
namespace BanSach.Components.Services.ReviewService
{

    public class ReviewService
    {
        private readonly AppDbContext _context;


        public ReviewService(AppDbContext context)
        {
            _context = context;
        }


        public async Task SaveReview(Review review)
        {
            Console.WriteLine($"Saving Review: Rating={review.Rating}, ReviewText={review.ReviewText}");

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
        }

    }

}