using ConsoleApp1;
using ConsoleApp1.Data;
using ConsoleApp1.Models;
using ConsoleApp1.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ClosedXML.Excel;
namespace SurveyApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Setup Dependency Injection
            var serviceProvider = new ServiceCollection()
                 .AddDbContext<SurveyContext>(options =>
                     options.UseSqlServer("Data Source=DESKTOP-CPJ3TCE\\SQLEXPRESS;Initial Catalog=OnlineSurvey11;Integrated Security=True;Encrypt=False;Trust Server Certificate=True")) // Set your connection string
                 .AddScoped<AuthenticationService>()
                 .AddScoped<SurveyService>()
                 .AddScoped<ResponseService>()
                 .BuildServiceProvider();

            await SeedDataAsync(serviceProvider);
            await RunMenuAsync(serviceProvider);
        }

        private static async Task SeedDataAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
                

                var context = scope.ServiceProvider.GetRequiredService<SurveyContext>();
                var dataSeed = new DataSeed(context);
            

            try
            {
                await context.Database.EnsureCreatedAsync();
                await dataSeed.SeedSurveysAsync("Data/mock_surveys.json");
                await dataSeed.SeedResponsesAsync("Data/mock_responses.json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred during data seeding: {ex.Message}");
            }
        }

        private static async Task RunMenuAsync(IServiceProvider serviceProvider)
        {
            var authService = serviceProvider.GetRequiredService<AuthenticationService>();
            var surveyService = serviceProvider.GetRequiredService<SurveyService>();
            var responseService = serviceProvider.GetRequiredService<ResponseService>();

            while (true)
            {
                Console.Clear();
                Console.WriteLine("1. Admin Login");
                Console.WriteLine("2. User Login");
                Console.WriteLine("3. Admin Signup");
                Console.WriteLine("4. User Signup");
                Console.WriteLine("5. Exit");
                Console.Write("Choose an option: ");
                var option = Console.ReadLine();

                switch (option)
                {
                    case "1":
                        await AdminLoginAsync(authService, surveyService);
                        break;
                    case "2":
                        await UserLoginAsync(authService, surveyService, responseService);
                        break;
                    case "3":
                        await AdminSignupAsync(authService);
                        break;
                    case "4":
                        await UserSignupAsync(authService);
                        break;
                    case "5":
                        return;
                    default:
                        Console.WriteLine("Invalid option. Press any key to try again...");
                        Console.ReadKey();
                        break;
                }
            }
        }
        private static async Task AdminLoginAsync(AuthenticationService authService, SurveyService surveyService)
        {
            Console.Clear();
            Console.Write("Admin email: ");
            var email = Console.ReadLine();
            Console.Write("Password: ");
            var password = Console.ReadLine();

            var admin = await authService.AdminLoginAsync(email, password);
            if (admin != null)
            {
                Console.WriteLine("Login successful!");
                await AdminMenuAsync(surveyService, admin); // Pass the logged-in admin to AdminMenuAsync
            }
            else
            {
                Console.WriteLine("Invalid credentials. Press any key to return...");
                Console.ReadKey();
            }
        }
        private static async Task AdminMenuAsync(SurveyService surveyService, Admin loggedInAdmin)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Admin Menu:");
                Console.WriteLine("1. Create Survey");
                Console.WriteLine("2. View Surveys");
                Console.WriteLine("3. Number of surveys taken within a time duration");
                Console.WriteLine("4. Survey result of one particular user with section-wise score");
                Console.WriteLine("5. Top 5 survey scorers with their response");
                Console.WriteLine("6. Bottom 5 survey scorers with their response");
                Console.WriteLine("7. Logout");
                Console.Write("Choose an option: ");
                var option = Console.ReadLine();

                switch (option)
                {
                    case "1":
                        await CreateSurveyAsync(surveyService, loggedInAdmin);
                        break;
                    case "2":
                        await ViewSurveysAsync(surveyService);
                        break;
                    case "3":
                        await ViewSurveysTakenWithinDurationAsync(surveyService);
                        break;
                    case "4":
                        await ViewSurveyResultForUserAsync(surveyService);
                        break;
                    case "5":
                        await ViewTopSurveyScorersAsync(surveyService);
                        break;
                    case "6":
                        await ViewBottomSurveyScorersAsync(surveyService);
                        break;
                    case "7":
                        return;
                    default:
                        Console.WriteLine("Invalid option. Press any key to try again...");
                        Console.ReadKey();
                        break;
                }
            }
        }
        private static async Task CreateSurveyAsync(SurveyService surveyService, Admin loggedInAdmin)
        {
            Console.Clear();
            Console.WriteLine("Creating a new survey...");
            Console.Write("Enter Survey Title: ");
            var title = Console.ReadLine();

            Console.Write("Enter Survey Description (optional): ");
            var description = Console.ReadLine();

            var survey = new Survey
            {
                Title = title,
                Description = description,
                AdminID = loggedInAdmin.AdminID,
                CreatedAt = DateTime.Now
            };

            await surveyService.CreateSurveyAsync(survey);

            // Add Sections
            Console.WriteLine("How many sections would you like to add?");
            int sectionCount = int.Parse(Console.ReadLine());
            for (int i = 0; i < sectionCount; i++)
            {
                Console.Write($"Enter Title for Section {i + 1}: ");
                var sectionTitle = Console.ReadLine();
                Console.WriteLine("Enter the description of the Section");
                var sectionDescription = Console.ReadLine();

                var section = new Section
                {
                    Description=sectionDescription,
                    Title = sectionTitle,
                    SurveyID = survey.SurveyID
                };

                await surveyService.AddSectionAsync(section);

                // Add Questions to the Section
                Console.WriteLine("How many questions would you like to add?");
                int questionCount = int.Parse(Console.ReadLine());
                for (int j = 0; j < questionCount; j++)
                {
                    Console.Write($"Enter Question {j + 1}: ");
                    var questionText = Console.ReadLine();

                    var question = new Question
                    {
                        Text = questionText,
                        SectionID = section.SectionID
                    };

                    await surveyService.AddQuestionAsync(question);

                    // Add Options to the Question

                    for (int k = 0; k < 4; k++)
                    {
                        Console.Write($"Enter Option {k + 1}: ");
                        var optionText = Console.ReadLine();

                        var option = new Option
                        {
                            Weightage = 5*((k + 1) * 10)/100,
                            Text = optionText,
                            QuestionID = question.QuestionID
                        };

                        await surveyService.AddOptionAsync(option);
                    }
                }
            }

            Console.WriteLine("Survey created successfully with sections, questions, and options!");
            Console.WriteLine("Press any key to return...");
            Console.ReadKey();
        }


        private static async Task ViewSurveysAsync(SurveyService surveyService)
        {
            var surveys = await surveyService.GetAllSurveysAsync();
            foreach (var survey in surveys)
            {
                Console.WriteLine($"Survey ID: {survey.SurveyID}, Title: {survey.Title}");
            }
            Console.WriteLine("Press any key to return...");
            Console.ReadKey();
        }
        private static async Task ViewSurveysTakenWithinDurationAsync(SurveyService surveyService)
        {
            Console.Write("Enter start date (yyyy-mm-dd): ");
            var startDate = DateTime.Parse(Console.ReadLine());
            Console.Write("Enter end date (yyyy-mm-dd): ");
            var endDate = DateTime.Parse(Console.ReadLine());

            // Fetch the survey count
            var count = await surveyService.GetSurveyCountWithinDurationAsync(startDate, endDate);

            // Create a new Excel workbook and worksheet
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Survey Report");

            // Add headers
            worksheet.Cell(1, 1).Value = "Survey Count Report";
            worksheet.Cell(2, 1).Value = "Start Date";
            worksheet.Cell(2, 2).Value = startDate.ToString("yyyy-MM-dd");
            worksheet.Cell(3, 1).Value = "End Date";
            worksheet.Cell(3, 2).Value = endDate.ToString("yyyy-MM-dd");
            worksheet.Cell(4, 1).Value = "Number of Surveys Taken";
            worksheet.Cell(4, 2).Value = count;

            // Save the workbook
            var filePath = "C:\\Users\\prade\\Desktop\\ConsoleApp1\\ConsoleApp1\\excel files\\SurveyCountReport.xlsx";
            workbook.SaveAs(filePath);

            Console.WriteLine($"Excel file '{filePath}' has been created.");
            Console.WriteLine("Press any key to return...");
            Console.ReadKey();
        }

        private static async Task ViewSurveyResultForUserAsync(SurveyService surveyService)
        {
            Console.Clear();
            Console.Write("Enter Survey ID: ");
            var surveyId = int.Parse(Console.ReadLine());
            Console.Write("Enter User ID: ");
            var userId = int.Parse(Console.ReadLine());

            // Fetch survey results for the user
            var results = await surveyService.GetSurveyResultForUserAsync(surveyId, userId);

            // Create a new Excel workbook and worksheet
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Survey Results");

            // Add headers
            worksheet.Cell(1, 1).Value = "Survey Results for User";
            worksheet.Cell(2, 1).Value = "Section Title";
            worksheet.Cell(2, 2).Value = "Section Score";

            // Add data using foreach loop
            int row = 3; // Start from the third row
            foreach (var result in results)
            {
                worksheet.Cell(row, 1).Value = result.SectionTitle;
                worksheet.Cell(row, 2).Value = result.SectionScore;
                row++;
            }

            // Save the workbook
            var filePath = "C:\\Users\\prade\\Desktop\\ConsoleApp1\\ConsoleApp1\\excel files\\SurveyResultsReport.xlsx";
            workbook.SaveAs(filePath);

            Console.WriteLine($"Excel file '{filePath}' has been created.");
            Console.WriteLine("Press any key to return...");
            Console.ReadKey();
        }

        private static async Task ViewTopSurveyScorersAsync(SurveyService surveyService)
        {
            var topScorers = await surveyService.GetTopSurveyScorersAsync(5);

            // Create a new Excel workbook
            using (var workbook = new XLWorkbook())
            {
                // Add a new worksheet to the workbook
                var worksheet = workbook.Worksheets.Add("Top Scorers");

                // Add headers to the worksheet
                worksheet.Cell(1, 1).Value = "User ID";
                worksheet.Cell(1, 2).Value = "Total Score";

                // Populate the worksheet with data
                for (int i = 0; i < topScorers.Count; i++)
                {
                    worksheet.Cell(i + 2, 1).Value = topScorers[i].UserID;
                    worksheet.Cell(i + 2, 2).Value = topScorers[i].TotalScore;
                }
                // Save the workbook to a file
                var filePath = "C:\\Users\\prade\\Desktop\\ConsoleApp1\\ConsoleApp1\\excel files\\TopSurveyScorers.xlsx";
                workbook.SaveAs(filePath);

                Console.WriteLine($"Data saved to {filePath} successfully.");
            }

            foreach (var scorer in topScorers)
            {
                Console.WriteLine($"User ID: {scorer.UserID}, Total Score: {scorer.TotalScore}");
            }

            Console.WriteLine("Press any key to return...");
            Console.ReadKey();
        }


        // 7. Bottom 5 survey scorers with their response
        private static async Task ViewBottomSurveyScorersAsync(SurveyService surveyService)
        {
            var bottomScorers = await surveyService.GetBottomSurveyScorersAsync(5);

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Bottom Scorers");

                // Add headers
                worksheet.Cell(1, 1).Value = "User ID";
                worksheet.Cell(1, 2).Value = "Total Score";

                // Add data
                for (int i = 0; i < bottomScorers.Count; i++)
                {
                    worksheet.Cell(i + 2, 1).Value = bottomScorers[i].UserID;
                    worksheet.Cell(i + 2, 2).Value = bottomScorers[i].TotalScore;
                }

                // Save the workbook
                var fileName = @"C:\Users\prade\Desktop\ConsoleApp1\ConsoleApp1\excel files\BottomSurveyScorers.xlsx";
                workbook.SaveAs(fileName);

                Console.WriteLine($"Data saved to {fileName}");
            }

            foreach (var scorer in bottomScorers)
            {
                Console.WriteLine($"User ID: {scorer.UserID}, Total Score: {scorer.TotalScore}");
            }

            Console.WriteLine("Press any key to return...");
            Console.ReadKey();
        }



        private static async Task UserLoginAsync(AuthenticationService authService, SurveyService surveyService, ResponseService responseService)
        {
            Console.Clear();
            Console.Write("User Email: ");
            var email = Console.ReadLine();
            Console.Write("Password: ");
            var password = Console.ReadLine();
            var user = await authService.UserLoginAsync(email, password);
            if (user != null)
            {
                Console.WriteLine("Login successful!");
                await UserMenuAsync(user.UserID, surveyService, responseService);
            }
            else
            {
                Console.WriteLine("Invalid credentials. Press any key to return...");
                Console.ReadKey();
            }
        }
        private static async Task UserMenuAsync(int userId, SurveyService surveyService, ResponseService responseService)
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("User Menu:");
                Console.WriteLine("1. View Surveys");
                Console.WriteLine("2. Attempt Survey");
                Console.WriteLine("3. Logout");
                Console.Write("Choose an option: ");
                var option = Console.ReadLine();

                switch (option)
                {
                    case "1":
                        await ViewSurveysAsync(surveyService);
                        break;
                    case "2":
                        await AttemptSurveyAsync(userId, surveyService, responseService);
                        break;
                    case "3":
                        return;
                    default:
                        Console.WriteLine("Invalid option. Press any key to try again...");
                        Console.ReadKey();
                        break;
                }
            }
        }

        private static async Task AttemptSurveyAsync(int userId, SurveyService surveyService, ResponseService responseService)
        {
            Console.Clear();
            Console.Write("Enter Survey ID to attempt: ");
            if (int.TryParse(Console.ReadLine(), out int surveyId))
            {
                var survey = await surveyService.GetSurveyByIdAsync(surveyId);
                if (survey != null)
                {
                    var response = new Response
                    {
                        UserID = userId,
                        SurveyID = surveyId,
                        SubmittedAt = DateTime.Now,
                        ResponseDetails = new List<ResponseDetail>()
                    };
                    Console.WriteLine($"Survey Title: {survey.Title}");

                    foreach (var section in survey.Sections)
                    {
                        Console.WriteLine($"Section: {section.Title}");
                        foreach (var question in section.Questions)
                        {
                            Console.WriteLine($"Question: {question.Text}");
                            for (int i = 0; i < question.Options.Count(); i++)
                            {
                                var option = question.Options.ElementAt(i);
                                Console.WriteLine($"{i + 1}. {option.Text}");
                            }

                            int selectedOptionId = await GetSelectedOptionIdAsync(question);
                            if (selectedOptionId != -1)
                            {
                                var responseDetail = new ResponseDetail
                                {
                                    QuestionID = question.QuestionID,
                                    OptionID = selectedOptionId
                                };
                                response.ResponseDetails.Add(responseDetail);
                            }
                        }
                    }

                    await responseService.SubmitResponseAsync(response);
                    Console.WriteLine("Survey submitted successfully!");
                }
                else
                {
                    Console.WriteLine("Survey not found.");
                }
            }
            else
            {
                Console.WriteLine("Invalid Survey ID.");
            }
            Console.WriteLine("Press any key to return...");
            Console.ReadKey();
        }
        private static async Task<int> GetSelectedOptionIdAsync(Question question)
        {
            Console.Write("Select an option (number): ");
            if (int.TryParse(Console.ReadLine(), out int optionIndex) && optionIndex >= 1 && optionIndex <= question.Options.Count())
            {
                return question.Options.ElementAt(optionIndex - 1).OptionID;
            }
            else
            {
                Console.WriteLine("Invalid selection. Please try again.");
                return await GetSelectedOptionIdAsync(question);
            }
        }

        private static async Task AdminSignupAsync(AuthenticationService authService)
        {
            Console.Clear();
            Console.Write("Admin Username: ");
            var username = Console.ReadLine();
            Console.Write("Password: ");
            var password = Console.ReadLine();

            var admin = await authService.AdminSignupAsync(username, password);
            if (admin != null)
            {
                Console.WriteLine("Signup successful!");
            }
            else
            {
                Console.WriteLine("Username already exists. Press any key to return...");
                Console.ReadKey();
            }
        }

        private static async Task UserSignupAsync(AuthenticationService authService)
        {
            Console.Clear();
           
            Console.Write("User Email: ");
            var email = Console.ReadLine();
            Console.WriteLine("Enter Password");
            var password = Console.ReadLine();

            var user = await authService.UserSignupAsync(email, password);
            if (user != null)
            {
                Console.WriteLine("Signup successful!");
            }
            else
            {
                Console.WriteLine("Username already exists. Press any key to return...");
                Console.ReadKey();
            }
        }
    }
}
