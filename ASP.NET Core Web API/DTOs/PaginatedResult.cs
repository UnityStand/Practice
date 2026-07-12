namespace ASP.NET_Core_Web_API.DTOs;

public class PaginatedResult<T>                                                                                                     
{                                                                                                                                   
    public int TotalCount { get; set; }                                                                                             
    public List<T> Items { get; set; } = [];                                                                                        
    public int Page { get; set; }                                                                                                   
    public int PageSize { get; set; }                                                                                               
}        