namespace Events.Application.Caching;

public class CacheOptions
{
   public TimeSpan EventTtl{get;set;}
   public TimeSpan TopEventsTtl{get;set;}
   
}