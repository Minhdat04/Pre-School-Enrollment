using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PreschoolEnrollmentSystem.Core.Entities;
using PreschoolEnrollmentSystem.Infrastructure.Data;
using PreschoolEnrollmentSystem.Infrastructure.Repositories.Interfaces;

namespace PreschoolEnrollmentSystem.Infrastructure.Repositories.Implementation
{
    public class Repository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly ApplicationDbContext _context;
        protected readonly DbSet<T> _dbSet;
        private IDbContextTransaction? _transaction;

        public Repository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<T>();
        }

        #region Read Operations

        public virtual async Task<T?> GetByIdAsync(Guid id)
        {
            return await _dbSet
                .Where(e => !e.IsDeleted) // Soft delete filter
                .FirstOrDefaultAsync(e => e.Id == id);
        }
        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet
                .Where(e => !e.IsDeleted) // Soft delete filter
                .ToListAsync();
        }
        public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet
                .Where(e => !e.IsDeleted) // Soft delete filter
                .Where(predicate)
                .ToListAsync();
        }
        public virtual async Task<T?> FindSingleAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet
                .Where(e => !e.IsDeleted) // Soft delete filter
                .Where(predicate)
                .SingleOrDefaultAsync();
        }
        public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
        {
            return await _dbSet
                .Where(e => !e.IsDeleted) // Soft delete filter
                .AnyAsync(predicate);
        }
        public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
        {
            var query = _dbSet.Where(e => !e.IsDeleted);

            if (predicate != null)
            {
                query = query.Where(predicate);
            }

            return await query.CountAsync();
        }
        public virtual IQueryable<T> Query()
        {
            return _dbSet.Where(e => !e.IsDeleted); // Soft delete filter
        }
        public virtual IQueryable<T> QueryWithDeleted()
        {
            return _dbSet.AsQueryable();
        }
        public virtual async Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(
            int pageNumber,
            int pageSize,
            Expression<Func<T, object>>? orderBy = null,
            bool ascending = true,
            Expression<Func<T, bool>>? filter = null)
        {
            // Validate parameters
            if (pageNumber < 1)
            {
                throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));
            }

            if (pageSize < 1 || pageSize > 1000)
            {
                throw new ArgumentException("Page size must be between 1 and 1000", nameof(pageSize));
            }

            // Start with base query (soft delete filter applied)
            IQueryable<T> query = Query();

            // Apply filter if provided
            if (filter != null)
            {
                query = query.Where(filter);
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync();

            // Apply ordering
            if (orderBy != null)
            {
                query = ascending
                    ? query.OrderBy(orderBy)
                    : query.OrderByDescending(orderBy);
            }
            else
            {
                // Default ordering by CreatedAt descending (newest first)
                query = query.OrderByDescending(e => e.CreatedAt);
            }

            // Apply pagination
            var items = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        #endregion

        #region Write Operations

        public virtual async Task<T> AddAsync(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            // Set audit fields if not already set
            if (entity.Id == Guid.Empty)
            {
                entity.Id = Guid.NewGuid();
            }

            if (entity.CreatedAt == default)
            {
                entity.CreatedAt = DateTime.UtcNow;
            }

            await _dbSet.AddAsync(entity);
            return entity;
        }
        public virtual async Task AddRangeAsync(IEnumerable<T> entities)
        {
            if (entities == null || !entities.Any())
            {
                throw new ArgumentException("Entities collection cannot be null or empty", nameof(entities));
            }

            // Set audit fields for all entities
            foreach (var entity in entities)
            {
                if (entity.Id == Guid.Empty)
                {
                    entity.Id = Guid.NewGuid();
                }

                if (entity.CreatedAt == default)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                }
            }

            await _dbSet.AddRangeAsync(entities);
        }
        public virtual void Update(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            // Set audit fields
            entity.UpdatedAt = DateTime.UtcNow;

            _dbSet.Update(entity);
        }
        public virtual void UpdateRange(IEnumerable<T> entities)
        {
            if (entities == null || !entities.Any())
            {
                throw new ArgumentException("Entities collection cannot be null or empty", nameof(entities));
            }

            // Set audit fields for all entities
            foreach (var entity in entities)
            {
                entity.UpdatedAt = DateTime.UtcNow;
            }

            _dbSet.UpdateRange(entities);
        }
        public virtual async Task<bool> DeleteAsync(Guid id, string deletedBy)
        {
            var entity = await _dbSet.FindAsync(id);

            if (entity == null)
            {
                return false;
            }

            return await DeleteAsync(entity, deletedBy);
        }
        public virtual async Task<bool> DeleteAsync(T entity, string deletedBy)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            if (string.IsNullOrWhiteSpace(deletedBy))
            {
                throw new ArgumentException("DeletedBy cannot be null or empty", nameof(deletedBy));
            }

            // Set soft delete fields
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            entity.DeletedBy = deletedBy;

            Update(entity);

            return await Task.FromResult(true);
        }
        public virtual void Remove(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            _dbSet.Remove(entity);
        }
        public virtual void RemoveRange(IEnumerable<T> entities)
        {
            if (entities == null || !entities.Any())
            {
                throw new ArgumentException("Entities collection cannot be null or empty", nameof(entities));
            }

            _dbSet.RemoveRange(entities);
        }

        #endregion

        #region Transaction Management

        public virtual async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
        public virtual async Task BeginTransactionAsync()
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("Transaction already started");
            }

            _transaction = await _context.Database.BeginTransactionAsync();
        }
        public virtual async Task CommitTransactionAsync()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No active transaction to commit");
            }

            try
            {
                await _transaction.CommitAsync();
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
        public virtual async Task RollbackTransactionAsync()
        {
            if (_transaction == null)
            {
                throw new InvalidOperationException("No active transaction to rollback");
            }

            try
            {
                await _transaction.RollbackAsync();
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        #endregion

        #region Entity State Management

        public virtual void Detach(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            _context.Entry(entity).State = EntityState.Detached;
        }
        public virtual void Attach(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            _dbSet.Attach(entity);
        }
        public virtual async Task ReloadAsync(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            await _context.Entry(entity).ReloadAsync();
        }

        #endregion

        #region IDisposable Support

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _transaction?.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}