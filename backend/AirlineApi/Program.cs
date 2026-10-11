using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Caching.Distributed;
using Shared;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

var builder=WebApplication.CreateBuilder(args);
var cs=builder.Configuration["POSTGRES_CONNECTION_STRING"]??"Host=localhost;Port=5432;Database=airline_intelligence;Username=airline;Password=airline_dev";
var secret=builder.Configuration["JWT_SECRET"]??"development-secret-at-least-32-characters";
var issuer=builder.Configuration["JWT_ISSUER"]??"AirlineIntelligence"; var audience=builder.Configuration["JWT_AUDIENCE"]??"AirlineIntelligence.Web";
builder.Services.AddDbContext<AirlineDbContext>(o=>o.UseNpgsql(cs));
builder.Services.AddStackExchangeRedisCache(o=>o.Configuration=builder.Configuration["REDIS_CONNECTION_STRING"]??"localhost:6379");
builder.Services.AddCors(o=>o.AddDefaultPolicy(p=>p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddEndpointsApiExplorer(); builder.Services.AddSwaggerGen();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o=>o.TokenValidationParameters=new(){ValidateIssuer=true,ValidIssuer=issuer,ValidateAudience=true,ValidAudience=audience,ValidateLifetime=true,IssuerSigningKey=new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),ValidateIssuerSigningKey=true});
builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddSingleton<JwtTokenService>();
var app=builder.Build();
app.Use(async(ctx,next)=>{try{await next();}catch(Exception ex){app.Logger.LogError(ex,"Unhandled API error");ctx.Response.StatusCode=500;await ctx.Response.WriteAsJsonAsync(new{message="An unexpected error occurred"});}});
app.UseSwagger(); app.UseSwaggerUI(); app.UseCors(); app.UseAuthentication(); app.UseAuthorization();
using(var scope=app.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AirlineDbContext>();for(var i=0;i<20;i++){try{await db.Database.MigrateAsync();await Seed.Run(db,builder.Configuration);break;}catch when(i<19){await Task.Delay(3000);}}}
/* Legacy minimal-endpoint implementation retained temporarily for migration history.
string Token(User u){var claims=new[]{new Claim(ClaimTypes.NameIdentifier,u.Id.ToString()),new Claim(ClaimTypes.Name,u.DisplayName),new Claim(ClaimTypes.Email,u.Email),new Claim(ClaimTypes.Role,u.Role)};var jwt=new JwtSecurityToken(issuer,audience,claims,expires:DateTime.UtcNow.AddHours(12),signingCredentials:new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),SecurityAlgorithms.HmacSha256));return new JwtSecurityTokenHandler().WriteToken(jwt);}
Guid Current(ClaimsPrincipal u)=>Guid.Parse(u.FindFirstValue(ClaimTypes.NameIdentifier)!);

app.MapPost("/api/auth/register",async(AuthRequest x,AirlineDbContext db)=>{if(string.IsNullOrWhiteSpace(x.Email)||x.Password.Length<8)return Results.BadRequest(new{message="Valid email and an 8 character password are required"});if(await db.Users.AnyAsync(u=>u.Email==x.Email.ToLower()))return Results.Conflict(new{message="Email already registered"});var u=new User{Email=x.Email.ToLower(),DisplayName=x.DisplayName?.Trim()??x.Email.Split('@')[0],PasswordHash=BCrypt.Net.BCrypt.HashPassword(x.Password)};db.Users.Add(u);await db.SaveChangesAsync();return Results.Created("/api/users/me",new{token=Token(u),user=Views.User(u)});});
app.MapPost("/api/auth/login",async(AuthRequest x,AirlineDbContext db)=>{var u=await db.Users.SingleOrDefaultAsync(u=>u.Email==x.Email.ToLower());return u is null||!BCrypt.Net.BCrypt.Verify(x.Password,u.PasswordHash)?Results.Unauthorized():Results.Ok(new{token=Token(u),user=Views.User(u)});});
app.MapPost("/api/auth/logout",()=>Results.Ok(new{message="Logged out"})).RequireAuthorization();
app.MapGet("/api/auth/me",async(ClaimsPrincipal p,AirlineDbContext db)=>Views.User(await db.Users.FindAsync(Current(p))??throw new InvalidOperationException("User not found"))).RequireAuthorization();
app.MapGet("/api/users/me",async(ClaimsPrincipal p,AirlineDbContext db)=>Views.User(await db.Users.FindAsync(Current(p))??throw new InvalidOperationException("User not found"))).RequireAuthorization();
app.MapGet("/api/users/me/saved-airlines",async(ClaimsPrincipal p,AirlineDbContext db)=>{var uid=Current(p);return await db.SavedAirlines.Where(s=>s.UserId==uid).Select(s=>new{s.AirlineId,s.Airline!.Name,s.Airline.IataCode,s.Airline.Country,s.Airline.Statistics!.OverallScore}).ToListAsync();}).RequireAuthorization();
app.MapPost("/api/users/me/saved-airlines",async(SaveRequest x,ClaimsPrincipal p,AirlineDbContext db)=>{var uid=Current(p);if(!await db.Airlines.AnyAsync(a=>a.Id==x.AirlineId))return Results.NotFound(new{message="Airline not found"});if(!await db.SavedAirlines.AnyAsync(s=>s.UserId==uid&&s.AirlineId==x.AirlineId)){db.SavedAirlines.Add(new(){UserId=uid,AirlineId=x.AirlineId});await db.SaveChangesAsync();}return Results.Created($"/api/airlines/{x.AirlineId}",new{x.AirlineId});}).RequireAuthorization();
app.MapDelete("/api/users/me/saved-airlines/{id:guid}",async(Guid id,ClaimsPrincipal p,AirlineDbContext db)=>{var s=await db.SavedAirlines.FindAsync(Current(p),id);if(s is null)return Results.NotFound();db.Remove(s);await db.SaveChangesAsync();return Results.NoContent();}).RequireAuthorization();

app.MapGet("/api/airlines",async(string? search,string? country,decimal? minScore,AirlineDbContext db)=>{var q=db.Airlines.Where(a=>a.IsActive).Include(a=>a.Statistics).AsQueryable();if(!string.IsNullOrWhiteSpace(search))q=q.Where(a=>a.Name.ToLower().Contains(search.ToLower())||a.IataCode.ToLower().Contains(search.ToLower()));if(!string.IsNullOrWhiteSpace(country))q=q.Where(a=>a.Country==country);if(minScore.HasValue)q=q.Where(a=>a.Statistics!.OverallScore>=minScore);return await q.OrderByDescending(a=>a.Statistics!.OverallScore).Select(a=>Views.Airline(a)).ToListAsync();});
app.MapGet("/api/airlines/{id:guid}",async(Guid id,AirlineDbContext db)=>{var a=await db.Airlines.Include(a=>a.Statistics).SingleOrDefaultAsync(a=>a.Id==id&&a.IsActive);return a is null?Results.NotFound(new{message="Airline not found"}):Results.Ok(Views.Airline(a));});
app.MapGet("/api/airlines/{id:guid}/statistics",async(Guid id,AirlineDbContext db,IDistributedCache cache,IConfiguration cfg)=>{var key=$"statistics:{id}";try{var cached=await cache.GetStringAsync(key);if(cached is not null)return Results.Content(cached,"application/json");}catch{}var s=await db.AirlineStatistics.SingleOrDefaultAsync(s=>s.AirlineId==id);if(s is null)return Results.NotFound(new{message="Statistics not found"});var history=await db.AirlineStatisticsHistory.Where(h=>h.AirlineId==id).OrderBy(h=>h.SnapshotAt).Select(h=>new{h.SnapshotAt,h.OverallScore,h.AverageRating,h.PositiveSentimentPercent,h.AveragePrice,h.EnvironmentalScore,h.FlightsPerWeek}).ToListAsync();var json=JsonSerializer.Serialize(new{current=s,history},new JsonSerializerOptions{PropertyNamingPolicy=JsonNamingPolicy.CamelCase});try{await cache.SetStringAsync(key,json,new DistributedCacheEntryOptions{AbsoluteExpirationRelativeToNow=TimeSpan.FromSeconds(cfg.GetValue("STATISTICS_CACHE_SECONDS",60))});}catch{}return Results.Content(json,"application/json");});
app.MapGet("/api/airlines/{id:guid}/reviews",(Guid id,AirlineDbContext db)=>db.Reviews.Where(r=>r.AirlineId==id).OrderByDescending(r=>r.CreatedAt).Select(r=>new{r.Id,r.Rating,r.ReviewText,r.CreatedAt,sentiment=r.SentimentResult!.Sentiment,topics=r.SentimentResult.Topics}).ToListAsync());
app.MapGet("/api/airlines/{id:guid}/prices",(Guid id,AirlineDbContext db)=>db.FlightPrices.Where(x=>x.AirlineId==id).OrderByDescending(x=>x.CollectedAt).Take(100).ToListAsync());
app.MapGet("/api/airlines/{id:guid}/flights",(Guid id,AirlineDbContext db)=>db.Flights.Where(x=>x.AirlineId==id).OrderBy(x=>x.DepartureTime).Take(100).ToListAsync());
app.MapGet("/api/airlines/{id:guid}/environment",(Guid id,AirlineDbContext db)=>db.EnvironmentalMeasurements.Where(x=>x.AirlineId==id).OrderByDescending(x=>x.MeasuredAt).Take(100).ToListAsync());
app.MapGet("/api/rankings",async(string? sort,AirlineDbContext db)=>{var q=db.Airlines.Where(a=>a.IsActive).Include(a=>a.Statistics).AsQueryable();q=sort switch{"price"=>q.OrderByDescending(a=>a.Statistics!.PriceScore),"service"=>q.OrderByDescending(a=>a.Statistics!.ServiceScore),"staff"=>q.OrderByDescending(a=>a.Statistics!.StaffScore),"environment"=>q.OrderByDescending(a=>a.Statistics!.EnvironmentalScore),"flights"=>q.OrderByDescending(a=>a.Statistics!.FlightsPerWeek),_=>q.OrderByDescending(a=>a.Statistics!.OverallScore)};return await q.Select(a=>Views.Airline(a)).ToListAsync();});
app.MapGet("/api/destinations",async(AirlineDbContext db)=>await db.Destinations.OrderBy(d=>d.City).ToListAsync());
app.MapGet("/api/destinations/{id:guid}",async(Guid id,AirlineDbContext db)=>await db.Destinations.FindAsync(id) is { } d?Results.Ok(d):Results.NotFound());
app.MapGet("/api/destinations/{id:guid}/airlines",async(Guid id,string? origin,DateTime? date,AirlineDbContext db)=>{var d=await db.Destinations.FindAsync(id);if(d is null)return Results.NotFound();var q=db.Flights.Where(f=>f.Destination==d.Code);if(!string.IsNullOrWhiteSpace(origin))q=q.Where(f=>f.Origin==origin);var ids=q.Select(f=>f.AirlineId).Distinct();var rows=await db.Airlines.Where(a=>ids.Contains(a.Id)).Select(a=>new{airline=Views.Airline(a),price=a.Prices.Where(p=>p.Destination==d.Code).OrderBy(p=>p.Price).Select(p=>(decimal?)p.Price).FirstOrDefault(),flights=a.Flights.Count(f=>f.Destination==d.Code),co2=a.EnvironmentalMeasurements.OrderByDescending(e=>e.MeasuredAt).Select(e=>(decimal?)e.Co2KgPerPassengerKm).FirstOrDefault()}).ToListAsync();return Results.Ok(rows);});

var admin=app.MapGroup("/api/admin").RequireAuthorization(p=>p.RequireRole("ADMIN"));
admin.MapGet("/airlines",(AirlineDbContext db)=>db.Airlines.Include(a=>a.Statistics).OrderBy(a=>a.Name).Select(a=>Views.Airline(a)).ToListAsync());
admin.MapPost("/airlines",async(AirlineRequest x,AirlineDbContext db)=>{if(await db.Airlines.AnyAsync(a=>a.IataCode==x.IataCode.ToUpper()))return Results.Conflict(new{message="IATA code already exists"});var a=new Airline{Name=x.Name,IataCode=x.IataCode.ToUpper(),Country=x.Country,IsActive=x.IsActive};db.Add(a);db.Add(new AirlineStatistic{AirlineId=a.Id});await db.SaveChangesAsync();return Results.Created($"/api/airlines/{a.Id}",Views.Airline(a));});
admin.MapPut("/airlines/{id:guid}",async(Guid id,AirlineRequest x,AirlineDbContext db)=>{var a=await db.Airlines.FindAsync(id);if(a is null)return Results.NotFound();a.Name=x.Name;a.IataCode=x.IataCode.ToUpper();a.Country=x.Country;a.IsActive=x.IsActive;await db.SaveChangesAsync();return Results.Ok(Views.Airline(a));});
admin.MapDelete("/airlines/{id:guid}",async(Guid id,AirlineDbContext db)=>{var a=await db.Airlines.FindAsync(id);if(a is null)return Results.NotFound();a.IsActive=false;await db.SaveChangesAsync();return Results.NoContent();});
admin.MapGet("/airlines/{id:guid}/notes",(Guid id,AirlineDbContext db)=>db.AdminNotes.Where(n=>n.AirlineId==id).OrderByDescending(n=>n.CreatedAt).Select(n=>new{n.Id,n.Content,n.CreatedAt,n.UpdatedAt,createdBy=n.User!.DisplayName}).ToListAsync());
admin.MapPost("/airlines/{id:guid}/notes",async(Guid id,NoteRequest x,ClaimsPrincipal p,AirlineDbContext db)=>{if(!await db.Airlines.AnyAsync(a=>a.Id==id))return Results.NotFound();var n=new AdminNote{AirlineId=id,UserId=Current(p),Content=x.Content};db.Add(n);await db.SaveChangesAsync();return Results.Created($"/api/admin/notes/{n.Id}",n);});
admin.MapPut("/notes/{id:guid}",async(Guid id,NoteRequest x,AirlineDbContext db)=>{var n=await db.AdminNotes.FindAsync(id);if(n is null)return Results.NotFound();n.Content=x.Content;n.UpdatedAt=DateTime.UtcNow;await db.SaveChangesAsync();return Results.Ok(n);});
admin.MapDelete("/notes/{id:guid}",async(Guid id,AirlineDbContext db)=>{var n=await db.AdminNotes.FindAsync(id);if(n is null)return Results.NotFound();db.Remove(n);await db.SaveChangesAsync();return Results.NoContent();});
admin.MapGet("/dashboard",async(AirlineDbContext db)=>new{totalAirlines=await db.Airlines.CountAsync(),totalReviews=await db.Reviews.CountAsync(),totalUsers=await db.Users.CountAsync(),totalFlights=await db.Flights.CountAsync(),lastDataCollection=await db.RawData.MaxAsync(x=>(DateTime?)x.ReceivedAt),pipelineErrors=await db.RawData.CountAsync(x=>x.Error!=null)});
app.MapGet("/health",()=>Results.Ok(new{status="healthy"})); */
app.MapControllers();
app.Run();

record AuthRequest(string Email,string Password,string? DisplayName); record SaveRequest(Guid AirlineId); record AirlineRequest(string Name,string IataCode,string Country,bool IsActive=true); record NoteRequest(string Content);
static class Views { public static object User(User u)=>new{u.Id,u.Email,u.DisplayName,u.Role,u.CreatedAt}; public static object Airline(Airline a)=>new{a.Id,a.Name,a.IataCode,a.Country,a.IsActive,statistics=a.Statistics is null?null:new{a.Statistics.OverallScore,a.Statistics.AverageRating,a.Statistics.PositiveSentimentPercent,a.Statistics.StaffScore,a.Statistics.ServiceScore,a.Statistics.AveragePrice,a.Statistics.LowestPrice,a.Statistics.HighestPrice,a.Statistics.FlightsPerWeek,a.Statistics.DestinationCount,a.Statistics.RouteCount,a.Statistics.EnvironmentalScore,a.Statistics.AverageCo2,a.Statistics.PassengerExperienceScore,a.Statistics.PriceScore,a.Statistics.AvailabilityScore,a.Statistics.PositiveTopics,a.Statistics.Complaints}}; }
public partial class Program{}
