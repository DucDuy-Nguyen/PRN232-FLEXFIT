using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Flexfit.DTOs;
using Flexfit.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Flexfit.Controllers
{
    [Route("api/categories")]
    [ApiController]
    [Authorize]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        /// <summary>
        /// Lấy danh sách toàn bộ danh mục lớp học
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            return Ok(categories);
        }

        /// <summary>
        /// Lấy thông tin chi tiết một danh mục theo ID
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategoryById(Guid id)
        {
            var category = await _categoryService.GetCategoryByIdAsync(id);
            if (category == null)
                return NotFound(new { message = "Không tìm thấy danh mục lớp học." });
            return Ok(category);
        }

        /// <summary>
        /// Tạo mới danh mục (Chỉ dành cho Admin hệ thống)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryRequest request)
        {
            try
            {
                var categoryId = await _categoryService.CreateCategoryAsync(request);
                return Ok(new { message = "Tạo danh mục thành công!", categoryId });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Cập nhật danh mục (Chỉ dành cho Admin hệ thống)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCategory(Guid id, [FromBody] UpdateCategoryRequest request)
        {
            try
            {
                await _categoryService.UpdateCategoryAsync(id, request);
                return Ok(new { message = "Cập nhật danh mục thành công!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Xóa danh mục (Chỉ dành cho Admin hệ thống)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            try
            {
                await _categoryService.DeleteCategoryAsync(id);
                return Ok(new { message = "Xóa danh mục thành công!" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
