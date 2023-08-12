using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using User.Manager.Service.Models;
using User.Manager.Service.Service;
using UserAPI.Models;
using UserAPI.Models.Authentication.Signup;
using UserAPI.Models.CheckToken;
using UserAPI.Models.DbContext;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
//Entity framework
//Kết nối database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));
//Sử dụng identity với IdentityUser là custom bằng UserAccount
builder.Services.AddIdentity<UserAccount, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

//Them phương thức kiểm tra email có tồn tại không
builder.Services.Configure<IdentityOptions>(options =>
    options.SignIn.RequireConfirmedEmail = true);

//Thêm authentication với JWT
builder.Services.AddAuthentication(option =>
{
    option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    option.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(option =>
{
    option.SaveToken = true;
    option.RequireHttpsMetadata = false;
    option.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        RequireExpirationTime = true,
        ValidIssuer = configuration["JWT:ValidIssuer"],
        ValidAudience = configuration["JWT:ValidAudience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]))

    };
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();

//Thêm mới cookie
builder.Services.AddSession(option =>
{
    option.IdleTimeout = TimeSpan.FromSeconds(30);
    option.Cookie.HttpOnly = true;
    option.Cookie.IsEssential = true;
});
//Cấu hình cookie
builder.Services.ConfigureApplicationCookie(option =>
{
    option.Cookie.Name = "Yahallo_Authentication";
    option.ExpireTimeSpan= TimeSpan.FromDays(7);
});

//Thêm cấu hình send email
var emailConfig = configuration
    .GetSection("EmailConfiguration")
    .Get<EmailConfigration>();
builder.Services.AddSingleton(emailConfig);

builder.Services.AddScoped<ICheckToken,CheckToken>();
builder.Services.AddScoped<IEmailService, EmailService>();  

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option => {
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "Auth Api", Version = "v1" });
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference=new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });
    
});




var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
