using System.Collections.Generic;
using System.Threading.Tasks;
using UmiHealthPOS.Models.DTOs;

namespace UmiHealthPOS.Services
{
    public interface IWebSearchService
    {
        Task<SearchResponseDto> SearchAsync(SearchRequestDto request);
        Task<AutoCompleteResponseDto> GetAutoCompleteAsync(AutoCompleteRequestDto request);
        Task<string> SummarizeContentAsync(string content, string query);
    }
}
