using System.ComponentModel.Design;
using Microsoft.EntityFrameworkCore;
using Motivation.Models;

namespace Motivation.Data.Repositories
{
    public class CommentsRepository : IRepository<Comment>
    {
        private readonly ApplicationDbContext _context;

        public CommentsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public IQueryable<Comment> Entries => _context.Comments.Include(c => c.Author);

        public async Task CreateAsync(Comment comment)
        {
            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Comment comment)
        {
            var commentExists = _context.Comments.Any(c => c.Id == comment.Id);
            if (commentExists)
            {
                _context.Comments.Update(comment);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteAsync(int commentId)
        {
            var comment = _context.Comments.FirstOrDefault(c => c.Id == commentId);
            if (comment == null) return;
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
        }
    }
}
