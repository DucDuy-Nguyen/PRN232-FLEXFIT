using FlexFit.Catalog.Repository.Data;
using FlexFit.Catalog.Repository.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FlexFit.Catalog.Repository.Repositories;

public class BranchRepository : IBranchRepository
{
    private readonly CatalogDbContext _db;
    public BranchRepository(CatalogDbContext db) => _db = db;

    public async Task<GymAmenity?> GetAmenityByIdAsync(Guid amenityId) =>
        await _db.GymAmenities.FindAsync(amenityId);

    public async Task<Branch?> GetByIdAsync(Guid id) =>
        await _db.Branches
            .Include(b => b.Amenities)
            .Include(b => b.BranchImages)
            .Include(b => b.BranchStaffs)
            .FirstOrDefaultAsync(b => b.BranchId == id);

    public async Task<IEnumerable<Branch>> GetByOwnerIdAsync(Guid ownerId) =>
        await _db.Branches
            .Include(b => b.Amenities)
            .Include(b => b.BranchImages)
            .Include(b => b.Gym)
            .Include(b => b.BranchStaffs)
            .Where(b => b.Gym.OwnerId == ownerId)
            .ToListAsync();

    public async Task<IEnumerable<Branch>> GetAllAsync() =>
        await _db.Branches
            .Include(b => b.Amenities)
            .Include(b => b.BranchImages)
            .Include(b => b.BranchStaffs)
            .ToListAsync();

    public async Task AddAsync(Branch branch)
    {
        await _db.Branches.AddAsync(branch);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Branch branch)
    {
        _db.Branches.Update(branch);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var branch = await _db.Branches
            .Include(b => b.Amenities)
            .FirstOrDefaultAsync(b => b.BranchId == id);

        if (branch == null) return;

        if (branch.Amenities != null)
        {
            branch.Amenities.Clear();
        }

        var branchImages = await _db.BranchImages.Where(i => i.BranchId == id).ToListAsync();
        if (branchImages.Any())
        {
            _db.BranchImages.RemoveRange(branchImages);
        }

        var branchStaffs = await _db.BranchStaffs.Where(s => s.BranchId == id).ToListAsync();
        if (branchStaffs.Any())
        {
            _db.BranchStaffs.RemoveRange(branchStaffs);
        }

        var gymSessions = await _db.GymSessions.Where(gs => gs.BranchId == id).ToListAsync();
        if (gymSessions.Any())
        {
            _db.GymSessions.RemoveRange(gymSessions);
        }

        _db.Branches.Remove(branch);
        await _db.SaveChangesAsync();
    }

    public async Task RemoveImagesByBranchIdAsync(Guid branchId)
    {
        var existingImages = _db.BranchImages.Where(img => img.BranchId == branchId);
        if (await existingImages.AnyAsync())
        {
            _db.BranchImages.RemoveRange(existingImages);
        }
    }

    public async Task<bool> IsStaffInBranchAsync(Guid staffId, Guid branchId) => 
        await _db.BranchStaffs.AnyAsync(bs => bs.StaffId == staffId && bs.BranchId == branchId);

    public async Task<BranchStaff?> GetBranchStaffAsync(Guid staffId, Guid branchId) => 
        await _db.BranchStaffs.FirstOrDefaultAsync(bs => bs.StaffId == staffId && bs.BranchId == branchId);

    public async Task<BranchStaff?> GetBranchStaffByBranchIdAsync(Guid branchId) => 
        await _db.BranchStaffs.FirstOrDefaultAsync(bs => bs.BranchId == branchId);

    public async Task AddBranchStaffAsync(BranchStaff branchStaff) => 
        await _db.BranchStaffs.AddAsync(branchStaff);

    public async Task RemoveBranchStaffAsync(BranchStaff branchStaff)
    {
        _db.BranchStaffs.Remove(branchStaff);
        await Task.CompletedTask;
    }

    public async Task RemoveStaffFromAllBranchesAsync(Guid staffId)
    {
        var assignments = _db.BranchStaffs.Where(bs => bs.StaffId == staffId);
        _db.BranchStaffs.RemoveRange(assignments);
    }

    public async Task<int> CountBranchesForStaffAsync(Guid staffId, Guid excludeBranchId) => 
        await _db.BranchStaffs.CountAsync(bs => bs.StaffId == staffId && bs.BranchId != excludeBranchId);

    public async Task<bool> CheckGymOwnershipAsync(Guid gymId, Guid userId)
    {
        return await _db.Gyms.AnyAsync(g => g.GymId == gymId && g.OwnerId == userId);
    }

    public async Task<bool> CheckBranchOwnershipAsync(Guid branchId, Guid userId)
    {
        return await _db.Branches
            .Include(b => b.Gym)
            .AnyAsync(b => b.BranchId == branchId && b.Gym.OwnerId == userId);
    }

    public async Task<IEnumerable<GymAmenity>> GetAllAmenitiesAsync() => 
        await _db.GymAmenities.ToListAsync();

    public async Task AddAmenityAsync(GymAmenity amenity)
    {
        await _db.GymAmenities.AddAsync(amenity);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> AmenityExistsAsync(string amenityName) =>
        await _db.GymAmenities.AnyAsync(a => a.AmenityName.ToLower() == amenityName.Trim().ToLower());

    public async Task UpdateAmenityAsync(GymAmenity amenity)
    {
        _db.GymAmenities.Update(amenity);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAmenityAsync(GymAmenity amenity)
    {
        _db.GymAmenities.Remove(amenity);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateBranchImagesDbAsync(Guid branchId, List<BranchImage> newImages)
    {
        var oldImages = await _db.BranchImages.Where(img => img.BranchId == branchId).ToListAsync();
        if (oldImages.Any())
        {
            _db.BranchImages.RemoveRange(oldImages);
        }

        if (newImages != null && newImages.Any())
        {
            await _db.BranchImages.AddRangeAsync(newImages);
        }

        await _db.SaveChangesAsync();
    }

    public async Task SaveChangesAsync() => await _db.SaveChangesAsync();
}


