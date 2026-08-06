using System.ComponentModel.DataAnnotations;

namespace ASP.NET_Core_Web_API.Models;

public class Event
{

    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public int TotalSeats { get; private set; } = 0;
    public int AvailableSeats { get; private set; }

    public static Event Create(string title, string? description, DateTime startAt, DateTime endAt, int totalSeats)                                                                               
    {                                                                                                                                                   
        if (totalSeats <= 0)                                                                                                                            
            throw new ValidationException("totalSeats cannot be less or equal to zero");          
                
                                                                                                                                                      
        return new Event                                                                                                                                
        {          
            Description = description,
            Title = title,                                                                                                                              
            TotalSeats = totalSeats,                                                                                                                    
            AvailableSeats = totalSeats,
            StartAt = startAt,
            EndAt = endAt
        };                                                                                                                                              
    }    

    public bool TryReserveSeats(int count = 1)
    {
        if ( AvailableSeats < count  )
        {
            return false;
        }
        else
        {
            AvailableSeats -= count;
            return true ;
        }

        
    }
    
    public bool ReleaseSeats(int count = 1)
    {
        if (AvailableSeats + count > TotalSeats)
        {
            return false;
        }
        else
        {
            AvailableSeats += count;
            return true ;
        }

        
    }
}