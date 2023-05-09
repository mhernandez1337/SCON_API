using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using WebSupplementalAPI;
using System.Data.SqlClient;
using System.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Cors.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//Enable Cors
builder.Services.AddCors(p => p.AddPolicy("corsapp", builder =>
{
    builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
}));



builder.Services.AddDbContext<DataContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
//Set Cors
app.UseCors("corsapp");
app.UseHttpsRedirection();

//Get all Oral Arguments
app.MapGet("/api/OralArguments", async (DataContext context) => {
    var response = await context.oralArgCalendar.ToArrayAsync();
    return response;
});

//Get all Oral Arguments
app.MapGet("/api/COAOralArguments", async (DataContext context) => {
    var response = await context.coaoralarguements.ToArrayAsync(); 
    return response;
});

//Get Judicial History Parameters
app.MapGet("/api/JudicialHistoryParameters", async (DataContext context) => {
    var judPositions = await context.positions.OrderBy(s => s.positionId).ToArrayAsync();
    var judCourts = await context.jhcourts.OrderBy(s => s.courtId).ToArrayAsync();
    var judDepartments = await context.departments.OrderBy(s => s.departmentId).ToArrayAsync();
    var judCounties = await context.counties.OrderBy(s => s.countyId).ToArrayAsync();
    var response = new object[] { judPositions, judCourts, judDepartments, judCounties };
    return response;
});

//Get Judical History All
app.MapPost("/api/JudicialHistory/Search", async (DataContext context, HttpContext httpcontext) => {

    //Read the HTTP request body which contains the court IDs passed from the frontend. 
    StreamReader stream = new StreamReader(httpcontext.Request.Body);
    string body = await stream.ReadToEndAsync();
    var searchParams = JsonObject.Parse(body);

    //Pull all records from the jud_hist_data table
    var response = await context.historyData.ToArrayAsync();

    if ((string)searchParams["first_name"] != null && (string)searchParams["last_name"] != null)
    {
        response = response.Where(s => s.first_name.Contains((string)searchParams["first_name"]) && s.last_name.Contains((string)searchParams["last_name"])).ToArray();
    }
    if ((string)searchParams["first_name"] != null && (string)searchParams["last_name"] == null)
    {
        response = response.Where(s => s.first_name.Contains((string)searchParams["first_name"])).ToArray();
    }
    if ((string)searchParams["first_name"] == null && (string)searchParams["last_name"] != null)
    {
        response = response.Where(s => s.last_name.Contains((string)searchParams["last_name"])).ToArray();
    }
    if ((string)searchParams["position"] != null)
    {
        response = response.Where(s => s.judicial_pos == (string)searchParams["position"]).ToArray();
    }
    if ((string)searchParams["court"] != null)
    {
        response = response.Where(s => s.courtName == (string)searchParams["court"]).ToArray();
    }
    if ((string)searchParams["department"] != null)
    {
        response = response.Where(s => s.department == (string)searchParams["department"]).ToArray();
    }
    if ((string)searchParams["county"] != null)
    {
        response = response.Where(s => s.county == (string)searchParams["county"]).ToArray();
    }
    int numRecord = response.Length;
    var temp = response.OrderBy(s => s.last_name).ThenBy(s => s.first_name).ThenBy(s => s.election_date).Skip(((int)searchParams["page_number"] - 1) * (int)searchParams["page_size"]).Take((int)searchParams["page_size"]);
    var tempResponse = new object[] { numRecord, temp };

    //return ;
    return tempResponse;
});

//Get Judical History by ID
app.MapGet("/api/JudicialHistory/Search/{id}", async (DataContext context, int id) => {
    var response = await context.historyData.Where(s => s.id == id).ToArrayAsync();
    return response[0];
});

//Get all Advance Opinions
app.MapGet("/api/AdvanceOpinions", async (DataContext context) => await context.advanceopinions.OrderByDescending(s => s.date.Year).ThenByDescending(s => s.advanceNumber).ToListAsync());

//Get all admin orders
app.MapGet("/api/AdminOrders", async (DataContext context) => await context.adminorders.OrderByDescending(s => s.date).ToListAsync());

//Get All Aging Cases
app.MapGet("/api/Aging-Submitted-Case-Report", async (DataContext context) => await context.agingcases.OrderByDescending(s => s.submissionDate).ThenBy(s => s.caseNumber).ToListAsync());

//Get all COA Unpublished Orders
app.MapGet("/api/COAUnpublishedOrders", async (DataContext context) => await context.coaunpublishedorders.OrderByDescending(s => s.date).ToListAsync());

//Get all Unpublished Orders
app.MapGet("/api/UnpublishedOrders", async (DataContext context) => await context.unpublishedorders.OrderByDescending(s => s.date).ToListAsync());

//Get statistics by one or two options and year
app.MapGet("/api/Statistics/{courtID1}/{courtID2}/{year}", async (int courtID1, int courtID2, int year, DataContext context) =>
{
    //Initalize an empty array that will hold all the data. Since no court equals 9999, the array will be an empty "Statistic" object.
    var tempVariable = await context.statistics.Where(s => s.courtID.Equals(999)).ToArrayAsync();
    var statisticData = tempVariable.Where(s => s.courtID.Equals(999));

    //Initalize variables to calculate whether an All Courts or Judicial District option was selected.
    int districtID1 = 0;
    bool districtSelected1 = false;
    int allCourtType1 = 0;
    bool allCourtTypeSelected1 = false;

    //A switch statement is ued to evaluate the first court id option that was sent over from the front end.
    switch (courtID1)
    {
        //All Courts
        case 9999:
            //We pull all the statistics data for all courts for all years and ordered by year.
            var tempAllCourts1 = await context.statistics.Where(s => s.year.Equals(year)).OrderBy(s => s.courtID).ToArrayAsync();

            //We generate a new Statistics object that we'll use to populate statistics data.
            Statistics allCourts = new Statistics();
            allCourts.juvFilings = 0;
            allCourts.famFilings = 0;
            allCourts.juvDispo = 0;
            allCourts.famDispo = 0;

            //Since all the data is pulled, we can then iterate through each record and add the individual stats to the allCourts statistics object. This will give us a single object with each year's statistics for all courts.
            for (int i = 0; i < tempAllCourts1.Length; i++)
            {
                //Add stats to blank allCourts stats object.
                allCourts.crimFilings += tempAllCourts1[i].crimFilings;
                allCourts.civilFilings += tempAllCourts1[i].civilFilings;
                allCourts.trafficFilings += tempAllCourts1[i].trafficFilings;
                allCourts.crimDispo += tempAllCourts1[i].crimDispo;
                allCourts.civilDispo += tempAllCourts1[i].civilDispo;
                allCourts.trafficDispo += tempAllCourts1[i].trafficDispo;

                //if statments are used for juvenile and family statistics to verify that NULL values aren't being read. If a null value is read, we simply add 0 to the running total for the allCourts statistics object.
                if (tempAllCourts1[i].juvFilings is null)
                {
                    allCourts.juvFilings += 0;
                }
                else
                {
                    allCourts.juvFilings += tempAllCourts1[i].juvFilings;
                }
                if (tempAllCourts1[i].famFilings is null)
                {
                    allCourts.famFilings += 0;
                }
                else
                {
                    allCourts.famFilings += tempAllCourts1[i].famFilings;
                }

                if (tempAllCourts1[i].juvDispo is null)
                {
                    allCourts.juvDispo += 0;
                }
                else
                {
                    allCourts.juvDispo += tempAllCourts1[i].juvDispo;
                }
                if (tempAllCourts1[i].famDispo is null)
                {
                    allCourts.famDispo += 0;
                }
                else
                {
                    allCourts.famDispo += tempAllCourts1[i].famDispo;
                }

            }
            //Include the year for the objects, which would be for a single year, and then append the calculated allCourts object to the empty statisticsData object, which will be returned at the end of the function.
            allCourts.year = year;
            allCourts.courtID = courtID1;
            statisticData = statisticData.Append(allCourts);
            break;
        //All District Courts
        case 8888:
            allCourtType1 = 1;
            allCourtTypeSelected1 = true;
            break;
        //All Justice Courts
        case 7777:
            allCourtType1 = 2;
            allCourtTypeSelected1 = true;
            break;
        //All Municipal Courts
        case 6666:
            allCourtType1 = 3;
            allCourtTypeSelected1 = true;
            break;
        //1st Judicial District
        case 10000:
            districtID1 = 1;
            districtSelected1 = true;
            break;
        //2nd Judicial District
        case 20000:
            districtID1 = 2;
            districtSelected1 = true;
            break;
        //3rd Judicial District
        case 30000:
            districtID1 = 3;
            districtSelected1 = true;
            break;
        //4th Judicial District
        case 40000:
            districtID1 = 4;
            districtSelected1 = true;
            break;
        //5th Judicial District
        case 50000:
            districtID1 = 5;
            districtSelected1 = true;
            break;
        //6th Judicial District
        case 60000:
            districtID1 = 6;
            districtSelected1 = true;
            break;
        //7th Judicial District
        case 70000:
            districtID1 = 7;
            districtSelected1 = true;
            break;
        //8th Judicial District
        case 80000:
            districtID1 = 8;
            districtSelected1 = true;
            break;
        //9th Judicial District
        case 90000:
            districtID1 = 9;
            districtSelected1 = true;
            break;
        //10th Judicial District
        case 100000:
            districtID1 = 10;
            districtSelected1 = true;
            break;
        //11th Judicial District
        case 110000:
            districtID1 = 11;
            districtSelected1 = true;
            break;
        //Default option for when an individual court is sent over.
        default:
            var courtRecordsbyID = await context.statistics.Where(s => s.courtID.Equals(courtID1)).ToArrayAsync();
            var courtRecordsbyYear = courtRecordsbyID.Where(s => s.year.Equals(year));
            if (courtRecordsbyYear.Count() == 0)
            {
                Statistics tempCourt = new Statistics();
                tempCourt.courtID = courtID1;
                tempCourt.year = year;
                tempCourt.civilDispo = 0;
                tempCourt.civilFilings = 0;
                tempCourt.crimDispo = 0;
                tempCourt.crimFilings = 0;
                tempCourt.famDispo = 0;
                tempCourt.famFilings = 0;
                tempCourt.juvDispo = 0;
                tempCourt.juvFilings = 0;
                tempCourt.trafficDispo = 0;
                tempCourt.trafficFilings = 0;
                statisticData = statisticData.Append(tempCourt);
            }
            else
            {
                statisticData = statisticData.Concat(courtRecordsbyYear);
            }
            break;

    }

    //We created a boolean to evaulate whether a judicial district was selected, and if it is, the below code is executed.
    if (districtSelected1)
    {
        //Get all of the court names within the given district
        var courtByDistrict = await context.courts.Where(s => s.districtId.Equals(districtID1)).ToArrayAsync();

        //Initilaize an empty Stats array.
        var statisticsByDistrict = await context.statistics.Where(s => s.courtID.Equals(999)).ToArrayAsync();
        var tempValue = statisticsByDistrict.Where(s => s.courtID.Equals(987654));//This will generate an empty Statistics array

        //We iterate through all of the courts in the provided judicial district, and we select the statistics data for each of the courts in the judicial district. Since all of the courts are stored in the courtByDistrict variable, we iterate through courtByDistrict and grab each courts ID, and use the court ID to pull all the statistics records for that court ID and year.
        for (var i = 0; i < courtByDistrict.Length; i++)
        {
            //Get all of the courts stats from the courtByDistrict object.
            var tempcourtByID = await context.statistics.Where(s => s.courtID.Equals(courtByDistrict[i].id)).ToArrayAsync();
            var tempCourtByYear = tempcourtByID.Where(s => s.year.Equals(year));

            //We concatenate the statistics data to tempValue. This will give us a list of all the statistics data with courts that have the same district ID and year.
            tempValue = tempValue.Concat(tempCourtByYear);
        }
        //Create a new array variable to hold the courts 
        var newValue = tempValue.ToArray();

        //We generate a new Statistics object that we'll use to populate statistics data.
        Statistics allCourts = new Statistics();
        allCourts.juvFilings = 0;
        allCourts.famFilings = 0;
        allCourts.juvDispo = 0;
        allCourts.famDispo = 0;

        //Since all the data is pulled, we can then iterate through each record and add the individual stats to the allCourts statistics object. This will give us a single object with each year's statistics for all courts.
        for (int i = 0; i < newValue.Length; i++)
        {
            //Add stats to blank stats object.
            allCourts.crimFilings += newValue[i].crimFilings;
            allCourts.civilFilings += newValue[i].civilFilings;
            allCourts.trafficFilings += newValue[i].trafficFilings;
            allCourts.crimDispo += newValue[i].crimDispo;
            allCourts.civilDispo += newValue[i].civilDispo;
            allCourts.trafficDispo += newValue[i].trafficDispo;

            //if else statments are used for juvenile and family statistics to verify that NULL values aren't being read. If a null value is read, we simply add 0 to the running total for the allCourts statistics object.
            if (newValue[i].juvFilings is null)
            {
                allCourts.juvFilings += 0;
            }
            else
            {
                allCourts.juvFilings += newValue[i].juvFilings;
            }
            if (newValue[i].famFilings is null)
            {
                allCourts.famFilings += 0;
            }
            else
            {
                allCourts.famFilings += newValue[i].famFilings;
            }
            
            if (newValue[i].juvDispo is null)
            {
                allCourts.juvDispo += 0;
            }
            else
            {
                allCourts.juvDispo += newValue[i].juvDispo;
            }
            if (newValue[i].famDispo is null)
            {
                allCourts.famDispo += 0;
            }
            else
            {
                allCourts.famDispo += newValue[i].famDispo;
            }
            
        }
        //Include the year for the objects, which would be for a single year, and then append the calculated allCourts object to the empty statisticsData object, which will be returned at the end of the function.
        allCourts.year = year;
        allCourts.courtID = courtID1;
        statisticData = statisticData.Append(allCourts);
    }

    //We created a boolean to evaulate whether a all court type (ex. all justice courts) was selected, and if it is, the below code is executed.
    if (allCourtTypeSelected1)
    {
        //Get all of the court names within the given district
        var courtByType = await context.courts.Where(s => s.typeId.Equals(allCourtType1)).ToArrayAsync();

        //Initilaize an empty Stats array.
        var statisticsByType = await context.statistics.Where(s => s.courtID.Equals(999)).ToArrayAsync();
        var tempValue = statisticsByType.Where(s => s.courtID.Equals(987654));//This will generate an empty Statistics array

        //We iterate through all of the courts in the provided court type, and we select the statistics data for each of the courts in the judicial district. Since all of the courts are stored in the courtByDistrict variable, we iterate through courtByDistrict and grab each courts ID, and use the court ID to pull all the statistics records for that court ID and year.
        for (var i = 0; i < courtByType.Length; i++)
        {
            //Get all of the courts stats from the courtByType object.
            var tempcourtByID = await context.statistics.Where(s => s.courtID.Equals(courtByType[i].id)).ToArrayAsync();
            var tempCourtByYear = tempcourtByID.Where(s => s.year.Equals(year));

            //We concatenate the statistics data to tempValue. This will give us a list of all the statistics data with courts that have the same district ID and year.
            tempValue = tempValue.Concat(tempCourtByYear);
        }
        //Create a new array variable to hold the courts 
        var newValue = tempValue.ToArray();

        //We generate a new Statistics object that we'll use to populate statistics data.
        Statistics allCourts = new Statistics();
        allCourts.juvFilings = 0;
        allCourts.famFilings = 0;
        allCourts.juvDispo = 0;
        allCourts.famDispo = 0;

        //Since all the data is pulled, we can then iterate through each record and add the individual stats to the allCourts statistics object. This will give us a single object with each year's statistics for all courts.
        for (int i = 0; i < newValue.Length; i++)
        {
            //Add stats to blank stats object.
            allCourts.crimFilings += newValue[i].crimFilings;
            allCourts.civilFilings += newValue[i].civilFilings;
            allCourts.trafficFilings += newValue[i].trafficFilings;
            allCourts.crimDispo += newValue[i].crimDispo;
            allCourts.civilDispo += newValue[i].civilDispo;
            allCourts.trafficDispo += newValue[i].trafficDispo;

            //if else statments are used for juvenile and family statistics to verify that NULL values aren't being read. If a null value is read, we simply add 0 to the running total for the allCourts statistics object.
            if (newValue[i].juvFilings is null)
            {
                allCourts.juvFilings += 0;
            }
            else
            {
                allCourts.juvFilings += newValue[i].juvFilings;
            }
            if (newValue[i].famFilings is null)
            {
                allCourts.famFilings += 0;
            }
            else
            {
                allCourts.famFilings += newValue[i].famFilings;
            }

            if (newValue[i].juvDispo is null)
            {
                allCourts.juvDispo += 0;
            }
            else
            {
                allCourts.juvDispo += newValue[i].juvDispo;
            }
            if (newValue[i].famDispo is null)
            {
                allCourts.famDispo += 0;
            }
            else
            {
                allCourts.famDispo += newValue[i].famDispo;
            }
        }
        //Include the year for the objects, which would be for a single year, and then append the calculated allCourts object to the empty statisticsData object, which will be returned at the end of the function.
        allCourts.year = year;
        allCourts.courtID = courtID1;
        statisticData = statisticData.Append(allCourts);
    }

    //Evaluate the second court option
    //Initalize variables to calculate whether an All Courts or Judicial District option was selected.
    int districtID2 = 0;
    bool districtSelected2 = false;
    int allCourtType2 = 0;
    bool allCourtTypeSelected2 = false;

    //A switch statement is ued to evaluate what second courtID option was sent over from the front end.
    if (courtID2 > 0)
    {
        switch (courtID2)
        {
            //All Courts
            case 9999:
                //We pull all the statistics data for all courts for all years and ordered by year.
                var tempAllCourts2 = await context.statistics.OrderBy(s => s.courtID).ToArrayAsync();

                //We generate a new Statistics object that we'll use to populate statistics data.
                Statistics allCourts = new Statistics();
                allCourts.juvFilings = 0;
                allCourts.famFilings = 0;
                allCourts.juvDispo = 0;
                allCourts.famDispo = 0;

                //Since all the data is pulled, we can then iterate through each record and add the individual stats to the allCourts statistics object. This will give us a single object with each year's statistics for all courts.
                for (int i = 0; i < tempAllCourts2.Length; i++)
                {
                    //Add stats to blank allCourts stats object.
                    allCourts.crimFilings += tempAllCourts2[i].crimFilings;
                    allCourts.civilFilings += tempAllCourts2[i].civilFilings;
                    allCourts.trafficFilings += tempAllCourts2[i].trafficFilings;
                    allCourts.crimDispo += tempAllCourts2[i].crimDispo;
                    allCourts.civilDispo += tempAllCourts2[i].civilDispo;
                    allCourts.trafficDispo += tempAllCourts2[i].trafficDispo;
                    //if statments are used for juvenile and family statistics to verify that NULL values aren't being read. If a null value is read, we simply add 0 to the running total for the allCourts statistics object.
                    if (tempAllCourts2[i].juvFilings is null)
                    {
                        allCourts.juvFilings += 0;
                    }
                    else
                    {
                        allCourts.juvFilings += tempAllCourts2[i].juvFilings;
                    }
                    if (tempAllCourts2[i].famFilings is null)
                    {
                        allCourts.famFilings += 0;
                    }
                    else
                    {
                        allCourts.famFilings += tempAllCourts2[i].famFilings;
                    }

                    if (tempAllCourts2[i].juvDispo is null)
                    {
                        allCourts.juvDispo += 0;
                    }
                    else
                    {
                        allCourts.juvDispo += tempAllCourts2[i].juvDispo;
                    }
                    if (tempAllCourts2[i].famDispo is null)
                    {
                        allCourts.famDispo += 0;
                    }
                    else
                    {
                        allCourts.famDispo += tempAllCourts2[i].famDispo;
                    }

                }
                //Include the year for the objects, which would be for a single year, and then append the calculated allCourts object to the empty statisticsData object, which will be returned at the end of the function.
                allCourts.year = year;
                allCourts.courtID = courtID2;
                statisticData = statisticData.Append(allCourts);
                break;
            //All District Courts
            case 8888:
                allCourtType2 = 1;
                allCourtTypeSelected2 = true;
                break;
            //All Justice Courts
            case 7777:
                allCourtType2 = 2;
                allCourtTypeSelected2 = true;
                break;
            //All Municipal Courts
            case 6666:
                allCourtType2 = 3;
                allCourtTypeSelected2 = true;
                break;
            //1st Judicial District
            case 10000:
                districtID2 = 1;
                districtSelected2 = true;
                break;
            //2nd Judicial District
            case 20000:
                districtID2 = 2;
                districtSelected2 = true;
                break;
            //3rd Judicial District
            case 30000:
                districtID2 = 3;
                districtSelected2 = true;
                break;
            //4th Judicial District
            case 40000:
                districtID2 = 4;
                districtSelected2 = true;
                break;
            //5th Judicial District
            case 50000:
                districtID2 = 5;
                districtSelected2 = true;
                break;
            //6th Judicial District
            case 60000:
                districtID2 = 6;
                districtSelected2 = true;
                break;
            //7th Judicial District
            case 70000:
                districtID2 = 7;
                districtSelected2 = true;
                break;
            //8th Judicial District
            case 80000:
                districtID2 = 8;
                districtSelected2 = true;
                break;
            //9th Judicial District
            case 90000:
                districtID2 = 9;
                districtSelected2 = true;
                break;
            //10th Judicial District
            case 100000:
                districtID2 = 10;
                districtSelected2 = true;
                break;
            //1th Judicial District
            case 110000:
                districtID2 = 11;
                districtSelected2 = true;
                break;
            //Default option for when an individual court is sent over.
            default:
                var courtRecordsbyID = await context.statistics.Where(s => s.courtID.Equals(courtID2)).ToArrayAsync();
                var courtRecordsbyYear = courtRecordsbyID.Where(s => s.year.Equals(year));
                if (courtRecordsbyYear.Count() == 0)
                {
                    Statistics tempCourt = new Statistics();
                    tempCourt.courtID = courtID2;
                    tempCourt.year = year;
                    tempCourt.civilDispo = 0;
                    tempCourt.civilFilings = 0;
                    tempCourt.crimDispo = 0;
                    tempCourt.crimFilings = 0;
                    tempCourt.famDispo = 0;
                    tempCourt.famFilings = 0;
                    tempCourt.juvDispo = 0;
                    tempCourt.juvFilings = 0;
                    tempCourt.trafficDispo = 0;
                    tempCourt.trafficFilings = 0;
                    statisticData = statisticData.Append(tempCourt);
                }
                else
                {
                    statisticData = statisticData.Concat(courtRecordsbyYear);
                }

                break;

        }
    }

    //We created a boolean to evaulate whether a judicial district was selected, and if it is, the below code is executed.
    if (districtSelected2)
    {
        //Get all of the court names within the given district
        var courtByDistrict = await context.courts.Where(s => s.districtId.Equals(districtID2)).ToArrayAsync();

        //Initilaize an empty Stats array.
        var statisticsByDistrict = await context.statistics.Where(s => s.courtID.Equals(999)).ToArrayAsync();
        var tempValue = statisticsByDistrict.Where(s => s.courtID.Equals(987654));//This will generate an empty Statistics array

        //We iterate through all of the courts in the provided judicial district, and we select the statistics data for each of the courts in the judicial district. Since all of the courts are stored in the courtByDistrict variable, we iterate through courtByDistrict and grab each courts ID, and use the court ID to pull all the statistics records for that court ID and year.
        for (var i = 0; i < courtByDistrict.Length; i++)
        {
            //Get all of the courts stats from the courtByDistrict object.
            var tempcourtByID = await context.statistics.Where(s => s.courtID.Equals(courtByDistrict[i].id)).ToArrayAsync();
            var tempCourtByYear = tempcourtByID.Where(s => s.year.Equals(year));

            //We concatenate the statistics data to tempValue. This will give us a list of all the statistics data with courts that have the same district ID and year.
            tempValue = tempValue.Concat(tempCourtByYear);
        }
        //Create a new array variable to hold the courts 
        var newValue = tempValue.ToArray();

        //We generate a new Statistics object that we'll use to populate statistics data.
        Statistics allCourts = new Statistics();
        allCourts.juvFilings = 0;
        allCourts.famFilings = 0;
        allCourts.juvDispo = 0;
        allCourts.famDispo = 0;

        //Since all the data is pulled, we can then iterate through each record and add the individual stats to the allCourts statistics object. This will give us a single object with each year's statistics for all courts.
        for (int i = 0; i < newValue.Length; i++)
        {
            //Add stats to blank stats object.
            allCourts.crimFilings += newValue[i].crimFilings;
            allCourts.civilFilings += newValue[i].civilFilings;
            allCourts.trafficFilings += newValue[i].trafficFilings;
            allCourts.crimDispo += newValue[i].crimDispo;
            allCourts.civilDispo += newValue[i].civilDispo;
            allCourts.trafficDispo += newValue[i].trafficDispo;

            //if else statments are used for juvenile and family statistics to verify that NULL values aren't being read. If a null value is read, we simply add 0 to the running total for the allCourts statistics object.
            if (newValue[i].juvFilings is null)
            {
                allCourts.juvFilings += 0;
            }
            else
            {
                allCourts.juvFilings += newValue[i].juvFilings;
            }
            if (newValue[i].famFilings is null)
            {
                allCourts.famFilings += 0;
            }
            else
            {
                allCourts.famFilings += newValue[i].famFilings;
            }

            if (newValue[i].juvDispo is null)
            {
                allCourts.juvDispo += 0;
            }
            else
            {
                allCourts.juvDispo += newValue[i].juvDispo;
            }
            if (newValue[i].famDispo is null)
            {
                allCourts.famDispo += 0;
            }
            else
            {
                allCourts.famDispo += newValue[i].famDispo;
            }

        }

        //Include the year for the objects, which would be for a single year, and then append the calculated allCourts object to the empty statisticsData object, which will be returned at the end of the function.
        allCourts.year = year;
        allCourts.courtID = courtID2;
        statisticData = statisticData.Append(allCourts);
    }

    //We created a boolean to evaulate whether a all court type (ex. all justice courts) was selected, and if it is, the below code is executed.
    if (allCourtTypeSelected2)
    {
        //Get all of the court names within the given court type
        var courtByType = await context.courts.Where(s => s.typeId.Equals(allCourtType2)).ToArrayAsync();

        //Initilaize an empty Stats array.
        var statisticsByType = await context.statistics.Where(s => s.courtID.Equals(999)).ToArrayAsync();
        var tempValue = statisticsByType.Where(s => s.courtID.Equals(987654));//This will generate an empty Statistics array

        //We iterate through all of the courts in the provided court type, and we select the statistics data for each of the courts in the judicial district. Since all of the courts are stored in the courtByDistrict variable, we iterate through courtByDistrict and grab each courts ID, and use the court ID to pull all the statistics records for that court ID and year.
        for (var i = 0; i < courtByType.Length; i++)
        {
            //Get all of the courts stats from the courtByType object.
            var tempcourtByID = await context.statistics.Where(s => s.courtID.Equals(courtByType[i].id)).ToArrayAsync();
            var tempCourtByYear = tempcourtByID.Where(s => s.year.Equals(year));

            //We concatenate the statistics data to tempValue. This will give us a list of all the statistics data with courts that have the same district ID and year.
            tempValue = tempValue.Concat(tempCourtByYear);
        }

        //Create a new array variable to hold the courts 
        var newValue = tempValue.ToArray();

        //We generate a new Statistics object that we'll use to populate statistics data.
        Statistics allCourts = new Statistics();
        allCourts.juvFilings = 0;
        allCourts.famFilings = 0;
        allCourts.juvDispo = 0;
        allCourts.famDispo = 0;

        //Since all the data is pulled, we can then iterate through each record and add the individual stats to the allCourts statistics object. This will give us a single object with each year's statistics for all courts.
        for (int i = 0; i < newValue.Length; i++)
        {
            //Add stats to blank stats object.
            allCourts.crimFilings += newValue[i].crimFilings;
            allCourts.civilFilings += newValue[i].civilFilings;
            allCourts.trafficFilings += newValue[i].trafficFilings;
            allCourts.crimDispo += newValue[i].crimDispo;
            allCourts.civilDispo += newValue[i].civilDispo;
            allCourts.trafficDispo += newValue[i].trafficDispo;

            //if else statments are used for juvenile and family statistics to verify that NULL values aren't being read. If a null value is read, we simply add 0 to the running total for the allCourts statistics object.
            if (newValue[i].juvFilings is null)
            {
                allCourts.juvFilings += 0;
            }
            else
            {
                allCourts.juvFilings += newValue[i].juvFilings;
            }
            if (newValue[i].famFilings is null)
            {
                allCourts.famFilings += 0;
            }
            else
            {
                allCourts.famFilings += newValue[i].famFilings;
            }

            if (newValue[i].juvDispo is null)
            {
                allCourts.juvDispo += 0;
            }
            else
            {
                allCourts.juvDispo += newValue[i].juvDispo;
            }
            if (newValue[i].famDispo is null)
            {
                allCourts.famDispo += 0;
            }
            else
            {
                allCourts.famDispo += newValue[i].famDispo;
            }
        }
        //Include the year for the objects, which would be for a single year, and then append the calculated allCourts object to the empty statisticsData object, which will be returned at the end of the function.
        allCourts.year = year;
        allCourts.courtID = courtID2;
        statisticData = statisticData.Append(allCourts);
    }

    //Initialize a response array to hold all of the calulated statisticData data.
    var response = statisticData.ToArray();

    //We'll interate through the array to verify that none of the family or juvenile records are NULL.
    for (int i = 0; i < response.Count(); i++)
    {
        if (response[i].famDispo is null)
        {
            response[i].famDispo = 0;
        }
        if (response[i].famFilings is null)
        {
            response[i].famFilings = 0;
        }
        if (response[i].juvDispo is null)
        {
            response[i].juvDispo = 0;
        }
        if (response[i].juvFilings is null)
        {
            response[i].juvFilings = 0;
        }
    }
    return response;
});

//Get All Courts
app.MapGet("/api/Courts", async (DataContext context) => await context.courts.OrderBy(s => s.typeId).ThenBy(s => s.courtname).ToListAsync());

//Get oldest and newest years of statistics. 
app.MapGet("/api/Statistics/YearSelection", async (DataContext context) =>
{
    var minYear = await context.statistics.OrderBy(s => s.year).Select(s => s.year).Take(1).ToArrayAsync();
    var maxYear = await context.statistics.OrderByDescending(s => s.year).Select(s => s.year).Take(1).ToArrayAsync();
    var yearList = minYear.Concat(maxYear);

    return yearList.ToList();
});

//Get Line Chart Data
app.MapGet("/api/Statistics/Line/{courtID}", async (int courtID, DataContext context) => 
{
    //Initalize an empty array that will hold all the data. Since no court equals 9999, the array will be an empty "Statistic" object.
    var tempstatisticData = await context.statistics.Where(s => s.courtID.Equals(999)).ToArrayAsync();
    var statisticData = tempstatisticData.Where(s => s.courtID.Equals(999));

    //Initalize variables to calculate whether an All Courts or Judicial District option was selected.
    int districtID = 0;
    bool districtSelected = false;
    int allCourtType = 0;
    bool allCourtTypeSelected = false;

    //A switch statement is ued to evaluate what option was sent over from the front end.
    switch (courtID)
    {   
        //All Courts
        case 9999:
            //We pull all the statistics data for all courts for all years and ordered by year.
            var tempAllCourts = await context.statistics.OrderByDescending(s => s.year).ToArrayAsync();
            bool newVar = true;

            //We generate a new Statistics object that we'll use to populate statistics data.
            Statistics allCourts = new Statistics();
            allCourts.juvFilings = 0;
            allCourts.famFilings = 0;
            allCourts.juvDispo = 0;
            allCourts.famDispo = 0;

            for (int i = 0; i < tempAllCourts.Length; i++)
             {
                //We initialize a new variable anytime the previous years has been fully calculated. 
                if (newVar)
                {
                    allCourts = new Statistics();
                    allCourts.juvFilings = 0;
                    allCourts.famFilings = 0;
                    allCourts.juvDispo = 0;
                    allCourts.famDispo = 0;
                    newVar = false;
                }

                //Add stats to blank stats object.
                allCourts.crimFilings += tempAllCourts[i].crimFilings;
                allCourts.civilFilings += tempAllCourts[i].civilFilings;
                allCourts.trafficFilings += tempAllCourts[i].trafficFilings;
                allCourts.crimDispo += tempAllCourts[i].crimDispo;
                allCourts.civilDispo += tempAllCourts[i].civilDispo;
                allCourts.trafficDispo += tempAllCourts[i].trafficDispo;

                //if statments are used for juvenile and family statistics to verify that NULL values aren't being read. If a null value is read, we simply add 0 to the running total for the allCourts statistics object.
                if (tempAllCourts[i].juvFilings is not null)
                 {
                     allCourts.juvFilings += tempAllCourts[i].juvFilings;
                 }

                 if (tempAllCourts[i].famFilings is not null)
                 {
                     allCourts.famFilings += tempAllCourts[i].famFilings;
                 }
                 if (tempAllCourts[i].juvDispo is not null)
                 {
                     allCourts.juvDispo += tempAllCourts[i].juvDispo;
                 }

                 if (tempAllCourts[i].famDispo is not null)
                 {
                     allCourts.famDispo += tempAllCourts[i].famDispo;
                 }


                //The following lines of code are used to evaluate if the next record has a different year than the current record. If so, we'll append the current allCourts variable to statisticData and then set the newVar boolean to true, which will declare a new Statistics variable that we'll use to calculate the next year's data. 
                 if ((i < (tempAllCourts.Length - 1)) && (tempAllCourts[(i + 1)].year != tempAllCourts[i].year))   
                 {
                        allCourts.courtID = 9999;
                        allCourts.year = tempAllCourts[i].year;
                        statisticData = statisticData.Append(allCourts);
                        newVar = true;
                 }
                else if (i == (tempAllCourts.Length - 1))
                {
                    allCourts.courtID = 9999;
                    allCourts.year = tempAllCourts[i].year;
                    statisticData = statisticData.Append(allCourts);
                }
             }

            break;
        //All District Courts
        case 8888:
            allCourtType = 1;
            allCourtTypeSelected = true;
            break;
        //All Justice Courts
        case 7777:
            allCourtType = 2;
            allCourtTypeSelected = true;
            break;
        //All Municipal Courts
        case 6666:
            allCourtType = 3;
            allCourtTypeSelected = true;
            break;
        //1st Judicial District
        case 10000:
            districtID = 1;
            districtSelected = true;
            break;
        //2nd Judicial District
        case 20000:
            districtID = 2;
            districtSelected = true;
            break;
        //3rd Judicial District
        case 30000:
            districtID = 3;
            districtSelected = true;
            break;
        //4th Judicial District
        case 40000:
            districtID = 4;
            districtSelected = true;
            break;
        //5th Judicial District
        case 50000:
            districtID = 5;
            districtSelected = true;
            break;
        //6th Judicial District
        case 60000:
            districtID = 6;
            districtSelected = true;
            break;
        //7th Judicial District
        case 70000:
            districtID = 7;
            districtSelected = true;
            break;
        //8th Judicial District
        case 80000:
            districtID = 8;
            districtSelected = true;
            break;
        //9th Judicial District
        case 90000:
            districtID = 9;
            districtSelected = true;
            break;
        //10th Judicial District
        case 100000:
            districtID = 10;
            districtSelected = true;
            break;
        //11th Judicial District
        case 110000:
            districtID = 11;
            districtSelected = true;
            break;
        //Default option for when an individual court is sent over.
        default:
            var courtRecordsbyID = await context.statistics.Where(s => s.courtID.Equals(courtID)).ToArrayAsync();
            var courtRecordsbyYear = courtRecordsbyID.OrderByDescending(s => s.year);
            statisticData = statisticData.Concat(courtRecordsbyYear);
            break;
    }

    //We created a boolean to evaulate whether a judicial district was selected, and if it is, the below code is executed.
    if (districtSelected)
    {
        //Get all of the court with the given district id number passed from the frontend.
        var courtByDistrict = await context.courts.Where(s => s.districtId.Equals(districtID)).ToArrayAsync();

        //Initilaize an empty Stats array.
        var statisticsByDistrict = await context.statistics.Where(s => s.courtID.Equals(999)).ToArrayAsync();
        var tempValue = statisticsByDistrict.Where(s => s.courtID.Equals(987654));//This will generate an empty Statistics array

        //We iterate through all of the courts in the provided judicial district, and we select the statistics data for each of the courts in the judicial district. Since all of the courts are stored in the courtByDistrict variable, we iterate through courtByDistrict and grab each courts ID, and use the court ID to pull all the statistics records for that court ID and year.
        for (var i = 0; i < courtByDistrict.Length; i++)
        {
            //Get all of the courts stats from the courtByDistrict object.
            var tempcourtByID = await context.statistics.Where(s => s.courtID.Equals(courtByDistrict[i].id)).ToArrayAsync();
            tempValue = tempValue.Concat(tempcourtByID);
        }
        tempValue = tempValue.OrderByDescending(s => s.year);
        var tempAllDistrictTypeCourts = tempValue.ToArray();

        //var tempAllDistrictTypeCourts = await context.statistics.OrderByDescending(s => s.year).ToArrayAsync();
        bool newVar = false;
        Statistics allCourts = new Statistics();
        allCourts.juvFilings = 0;
        allCourts.famFilings = 0;
        allCourts.juvDispo = 0;
        allCourts.famDispo = 0;

        for (int i = 0; i < tempAllDistrictTypeCourts.Length; i++)
        {
            //We initialize a new variable anytime the previous years has been fully calculated.
            if (newVar)
            {
                allCourts = new Statistics();
                allCourts.juvFilings = 0;
                allCourts.famFilings = 0;
                allCourts.juvDispo = 0;
                allCourts.famDispo = 0;
                newVar = false;
            }

            //Add stats to blank stats object.
            allCourts.crimFilings += tempAllDistrictTypeCourts[i].crimFilings;
            allCourts.civilFilings += tempAllDistrictTypeCourts[i].civilFilings;
            allCourts.trafficFilings += tempAllDistrictTypeCourts[i].trafficFilings;
            allCourts.crimDispo += tempAllDistrictTypeCourts[i].crimDispo;
            allCourts.civilDispo += tempAllDistrictTypeCourts[i].civilDispo;
            allCourts.trafficDispo += tempAllDistrictTypeCourts[i].trafficDispo;

            //if statements are used for juvenile and family statistics to verify that NULL values aren't being read. If a null value is read, we simply add 0 to the running total for the allCourts statistics object.
            if (tempAllDistrictTypeCourts[i].juvFilings is not null)
            {
                allCourts.juvFilings += tempAllDistrictTypeCourts[i].juvFilings;
            }

            if (tempAllDistrictTypeCourts[i].famFilings is not null)
            {
                allCourts.famFilings += tempAllDistrictTypeCourts[i].famFilings;
            }
            if (tempAllDistrictTypeCourts[i].juvDispo is not null)
            {
                allCourts.juvDispo += tempAllDistrictTypeCourts[i].juvDispo;
            }

            if (tempAllDistrictTypeCourts[i].famDispo is not null)
            {
                allCourts.famDispo += tempAllDistrictTypeCourts[i].famDispo;
            }

            //The following lines of code are used to evaluate if the next record has a different year than the current record. If so, we'll append the current allCourts variable to statisticData and then set the newVar boolean to true, which will declare a new Statistics variable that we'll use to calculate the next year's data. 
            if ((i < (tempAllDistrictTypeCourts.Length - 1)) && (tempAllDistrictTypeCourts[(i + 1)].year != tempAllDistrictTypeCourts[i].year))
            {
                allCourts.courtID = 9999;
                allCourts.year = tempAllDistrictTypeCourts[i].year;
                statisticData = statisticData.Append(allCourts);
                newVar = true;
            }
            else if (i == (tempAllDistrictTypeCourts.Length - 1))
            {
                allCourts.courtID = 9999;
                allCourts.year = tempAllDistrictTypeCourts[i].year;
                statisticData = statisticData.Append(allCourts);
            }
        }
    }

    if (allCourtTypeSelected)
    {
        //Get all of the court names within the given district
        var courtByType = await context.courts.Where(s => s.typeId.Equals(allCourtType)).ToArrayAsync();

        //Initilaize an empty Stats array.
        var statisticsByDistrict = await context.statistics.Where(s => s.courtID.Equals(999)).ToArrayAsync();
        var tempValue = statisticsByDistrict.Where(s => s.courtID.Equals(987654));//This will generate an empty Statistics array


        for (var i = 0; i < courtByType.Length; i++)
        {
            //Get all of the courts stats from the courtByDistrict object.
            var tempcourtByID = await context.statistics.Where(s => s.courtID.Equals(courtByType[i].id)).ToArrayAsync();
            //var tempcourtByYear = tempcourtByID.OrderByDescending(s => s.year);
            tempValue = tempValue.Concat(tempcourtByID);
        }

        tempValue = tempValue.OrderByDescending(s => s.year);

        var tempAllTypeCourts = tempValue.ToArray();

        //var tempAllDistrictTypeCourts = await context.statistics.OrderByDescending(s => s.year).ToArrayAsync();
        bool newVar = false;
        Statistics allCourts = new Statistics();
        allCourts.juvFilings = 0;
        allCourts.famFilings = 0;
        allCourts.juvDispo = 0;
        allCourts.famDispo = 0;

        for (int i = 0; i < tempAllTypeCourts.Length; i++)
        {
            //We initialize a new variable anytime the previous years has been fully calculated. 
            if (newVar)
            {
                allCourts = new Statistics();
                allCourts.juvFilings = 0;
                allCourts.famFilings = 0;
                allCourts.juvDispo = 0;
                allCourts.famDispo = 0;
                newVar = false;
            }

            //Add stats to blank stats object.
            allCourts.crimFilings += tempAllTypeCourts[i].crimFilings;
            allCourts.civilFilings += tempAllTypeCourts[i].civilFilings;
            allCourts.trafficFilings += tempAllTypeCourts[i].trafficFilings;
            allCourts.crimDispo += tempAllTypeCourts[i].crimDispo;
            allCourts.civilDispo += tempAllTypeCourts[i].civilDispo;
            allCourts.trafficDispo += tempAllTypeCourts[i].trafficDispo;

            //if statements are used for juvenile and family statistics to verify that NULL values aren't being read. If a null value is read, we simply add 0 to the running total for the allCourts statistics object
            if (tempAllTypeCourts[i].juvFilings is not null)
            {
                allCourts.juvFilings += tempAllTypeCourts[i].juvFilings;
            }

            if (tempAllTypeCourts[i].famFilings is not null)
            {
                allCourts.famFilings += tempAllTypeCourts[i].famFilings;
            }
            if (tempAllTypeCourts[i].juvDispo is not null)
            {
                allCourts.juvDispo += tempAllTypeCourts[i].juvDispo;
            }

            if (tempAllTypeCourts[i].famDispo is not null)
            {
                allCourts.famDispo += tempAllTypeCourts[i].famDispo;
            }

            //The following lines of code are used to evaluate if the next record has a different year than the current record. If so, we'll append the current allCourts variable to statisticData and then set the newVar boolean to true, which will declare a new Statistics variable that we'll use to calculate the next year's data. 
            if ((i < (tempAllTypeCourts.Length - 1)) && (tempAllTypeCourts[(i + 1)].year != tempAllTypeCourts[i].year))
            {
                allCourts.courtID = 9991;
                allCourts.year = tempAllTypeCourts[i].year;
                statisticData = statisticData.Append(allCourts);
                newVar = true;
            }
            else if (i == (tempAllTypeCourts.Length - 1))
            {
                allCourts.courtID = 9999;
                allCourts.year = tempAllTypeCourts[i].year;
                statisticData = statisticData.Append(allCourts);
            }
        }

        //statisticData = statisticData.Append(allCourts);
    }

    //Initialize a response array to hold all of the calulated statisticData data.
    var response = statisticData.ToArray();

    //We'll interate through the array to verify that none of the family or juvenile records are NULL.
    for (int i = 0; i < response.Count(); i++)
    {
        if (response[i].famDispo is null)
        {
            response[i].famDispo = 0;
        }
        if (response[i].famFilings is null)
        {
            response[i].famFilings = 0;
        }
        if (response[i].juvDispo is null)
        {
            response[i].juvDispo = 0;
        }
        if (response[i].juvFilings is null)
        {
            response[i].juvFilings = 0;
        }
    }
    return response;

});

//Get Line Chart Data
app.MapPost("/api/Statistics/Bar/{year}", async (int year, DataContext context, HttpContext httpcontext) =>
{
    //Initalize an empty array that will hold all the data. Since no court equals 9999, the array will be an empty "Statistic" object.
    var tempstatisticData = await context.statistics.Where(s => s.courtID.Equals(999)).ToArrayAsync();
    var statisticData = tempstatisticData.Where(s => s.courtID.Equals(999));

    //Initalize variables to calculate whether an All Courts or Judicial District option was selected.
    int districtID = 0;
    bool districtSelected = false;
    int allCourtType = 0;
    bool allCourtTypeSelected = false;

    //Read the HTTP request body which contains the court IDs passed from the frontend. 
    StreamReader stream = new StreamReader(httpcontext.Request.Body);
    string body = await stream.ReadToEndAsync();
    var courts = JsonObject.Parse(body);

    //Get the number of objects in the body, so that we can iterate through them.
    JsonArray items = (JsonArray)courts["courtIDs"];

    //Iterate through all the options passed from the frontend. Each option is evaluated and added to the empty statisticsData variable.
    for (int i = 0; i < items.Count; i++)
    {
        districtSelected = false;
        allCourtTypeSelected = false;

        //We generate a new Statistics object that we'll use to populate statistics data.
        Statistics allCourts = new Statistics();
        allCourts.juvFilings = 0;
        allCourts.famFilings = 0;
        allCourts.juvDispo = 0;
        allCourts.famDispo = 0;

        //A switch statement is ued to evaluate what option was sent over from the front end.
        switch ((int)courts["courtIDs"][i])
        {
            //All Courts option passed from frontend.
            case 9999:
                //We pull all the statistics data for a given year and order them by the court ID
                var tempAllCourts = await context.statistics.Where(s => s.year.Equals(year)).OrderBy(s => s.courtID).ToArrayAsync();

                //Since all the data is pulled, we can then iterate through each record and add the individual stats to the allCourts statistics object. This will give us a single object with each year's statistics for all courts.
                for (int j = 0; j < tempAllCourts.Length; j++)
                {
                    allCourts.crimFilings += tempAllCourts[j].crimFilings;
                    allCourts.civilFilings += tempAllCourts[j].civilFilings;
                    allCourts.trafficFilings += tempAllCourts[j].trafficFilings;
                    allCourts.crimDispo += tempAllCourts[j].crimDispo;
                    allCourts.civilDispo += tempAllCourts[j].civilDispo;
                    allCourts.trafficDispo += tempAllCourts[j].trafficDispo;
                    
                    //if else statments are used for juvenile and family statistics to verify that NULL values aren't being read. If a null value is read, we simply add 0 to the running total for the allCourts statistics object.
                    if (tempAllCourts[j].juvFilings is null)
                    {
                        allCourts.juvFilings += 0;
                    }
                    else
                    {
                        allCourts.juvFilings += tempAllCourts[j].juvFilings;
                    }
                    if (tempAllCourts[j].famFilings is null)
                    {
                        allCourts.famFilings += 0;
                    }
                    else
                    {
                        allCourts.famFilings += tempAllCourts[j].famFilings;
                    }

                    if (tempAllCourts[j].juvDispo is null)
                    {
                        allCourts.juvDispo += 0;
                    }
                    else
                    {
                        allCourts.juvDispo += tempAllCourts[j].juvDispo;
                    }
                    if (tempAllCourts[j].famDispo is null)
                    {
                        allCourts.famDispo += 0;
                    }
                    else
                    {
                        allCourts.famDispo += tempAllCourts[j].famDispo;
                    }

                }
                //Include the year for the objects, which would be for a single year, and then append the calculated allCourts object to the empty statisticsData object, which will be returned at the end of the function.
                allCourts.year = year;
                allCourts.courtID = (int)courts["courtIDs"][i];
                statisticData = statisticData.Append(allCourts);
                break;
            //All District Courts
            case 8888:
                allCourtType = 1;
                allCourtTypeSelected = true;
                break;
            //All Justice Courts
            case 7777:
                allCourtType = 2;
                allCourtTypeSelected = true;
                break;
            //All Municipal Courts
            case 6666:
                allCourtType = 3;
                allCourtTypeSelected = true;
                break;
            //1st Judicial District Court
            case 10000:
                districtID = 1;
                districtSelected = true;
                break;
            //2nd Judicial District Court
            case 20000:
                districtID = 2;
                districtSelected = true;
                break;
            //3rd Judicial District Court
            case 30000:
                districtID = 3;
                districtSelected = true;
                break;
            //4th Judicial District Court
            case 40000:
                districtID = 4;
                districtSelected = true;
                break;
            //5th Judicial District Court
            case 50000:
                districtID = 5;
                districtSelected = true;
                break;
            //6th Judicial District Court
            case 60000:
                districtID = 6;
                districtSelected = true;
                break;
            //7th Judicial District Court
            case 70000:
                districtID = 7;
                districtSelected = true;
                break;
            //8th Judicial District Court
            case 80000:
                districtID = 8;
                districtSelected = true;
                break;
            //9th Judicial District Court
            case 90000:
                districtID = 9;
                districtSelected = true;
                break;
            //10th Judicial District Court
            case 100000:
                districtID = 10;
                districtSelected = true;
                break;
            //11th Judicial District Court
            case 110000:
                districtID = 11;
                districtSelected = true;
                break;
            //Default option for when an individual court is sent over.
            default:
                var tempCourtById = await context.statistics.Where(s => s.courtID.Equals((int)courts["courtIDs"][i])).ToArrayAsync();
                var tempCourtByYear = tempCourtById.Where(s => s.year.Equals(year));
                if (tempCourtByYear.Count() == 0)
                {
                    Statistics tempCourt = new Statistics();
                    tempCourt.courtID = (int)courts["courtIDs"][i];
                    tempCourt.year = year;
                    tempCourt.civilDispo = 0;
                    tempCourt.civilFilings = 0;
                    tempCourt.crimDispo = 0;
                    tempCourt.crimFilings = 0;
                    tempCourt.famDispo = 0;
                    tempCourt.famFilings = 0;
                    tempCourt.juvDispo = 0;
                    tempCourt.juvFilings = 0;
                    tempCourt.trafficDispo = 0;
                    tempCourt.trafficFilings = 0;
                    statisticData = statisticData.Append(tempCourt);
                }
                else
                {
                    statisticData = statisticData.Concat(tempCourtByYear);
                }
                break;

        }
        
        //We created a boolean to evaulate whether a judicial district was selected, and if it is, the below code is executed.
        if (districtSelected)
        {
            //Get all of the court with the given district id number passed from the frontend.
            var courtByDistrict = await context.courts.Where(s => s.districtId.Equals(districtID)).ToArrayAsync();

            //Initilaize an empty Stats array. 
            var statisticsByDistrict = await context.statistics.Where(s => s.courtID.Equals(999)).ToArrayAsync();
            var tempValue = statisticsByDistrict.Where(s => s.courtID.Equals(987654));//This will generate an empty Statistics array

            //We iterate through all of the courts in the provided judicial district, and we select the statistics data for each of the courts in the judicial district. Since all of the courts are stored in the courtByDistrict variable, we iterate through courtByDistrict and grab each courts ID, and use the court ID to pull all the statistics records for that court ID and year.
            for (var j = 0; j < courtByDistrict.Length; j++)
            {
                //Get all of the courts stats from the courtByDistrict object.
                var tempcourtByID = await context.statistics.Where(s => s.courtID.Equals(courtByDistrict[j].id)).ToArrayAsync();
                var tempCourtByYear = tempcourtByID.Where(s => s.year.Equals(year));
                
                //We concatenate the statistics data to tempValue. This will give us a list of all the statistics data with courts that have the same district ID and year.
                tempValue = tempValue.Concat(tempCourtByYear);
            }
            //Create a new array variable to hold the courts 
            var newValue = tempValue.ToArray();

            //Since all the data is pulled, we can then iterate through each record and add the individual stats to the allCourts statistics object. This will give us a single object with each year's statistics for all courts.
            for (int j = 0; j < newValue.Length; j++)
            {
                allCourts.crimFilings += newValue[j].crimFilings;
                allCourts.civilFilings += newValue[j].civilFilings;
                allCourts.trafficFilings += newValue[j].trafficFilings;
                allCourts.crimDispo += newValue[j].crimDispo;
                allCourts.civilDispo += newValue[j].civilDispo;
                allCourts.trafficDispo += newValue[j].trafficDispo;
                //if else statments are used for juvenile and family statistics to verify that NULL values aren't being read. If a null value is read, we simply add 0 to the running total for the allCourts statistics object.
                if (newValue[j].juvFilings is null)
                {
                    allCourts.juvFilings += 0;
                }
                else
                {
                    allCourts.juvFilings += newValue[j].juvFilings;
                }
                if (newValue[j].famFilings is null)
                {
                    allCourts.famFilings += 0;
                }
                else
                {
                    allCourts.famFilings += newValue[j].famFilings;
                }

                if (newValue[j].juvDispo is null)
                {
                    allCourts.juvDispo += 0;
                }
                else
                {
                    allCourts.juvDispo += newValue[j].juvDispo;
                }
                if (newValue[j].famDispo is null)
                {
                    allCourts.famDispo += 0;
                }
                else
                {
                    allCourts.famDispo += newValue[j].famDispo;
                }

            }
            //Include the year for the objects, which would be for a single year, and then append the calculated allCourts object to the empty statisticsData object, which will be returned at the end of the function.
            allCourts.year = year;
            allCourts.courtID = (int)courts["courtIDs"][i];
            statisticData = statisticData.Append(allCourts);
        }

        //We created a boolean to evaulate whether a all court type (ex. all justice courts) was selected, and if it is, the below code is executed.
        if (allCourtTypeSelected)
        {
            //Get all of the court names within the given district
            var courtByType = await context.courts.Where(s => s.typeId.Equals(allCourtType)).ToArrayAsync();

            //Initilaize an empty Stats array.
            var statisticsByDistrict = await context.statistics.Where(s => s.courtID.Equals(999)).ToArrayAsync();
            var tempValue = statisticsByDistrict.Where(s => s.courtID.Equals(987654));//This will generate an empty Statistics array


            for (var j = 0; j < courtByType.Length; j++)
            {
                //Get all of the courts stats from the courtByType object.
                var tempcourtByID = await context.statistics.Where(s => s.courtID.Equals(courtByType[j].id)).ToArrayAsync();
                var tempCourtByYear = tempcourtByID.Where(s => s.year.Equals(year));

                //We concatenate the statistics data to tempValue. This will give us a list of all the statistics data with courts that have the same district ID and year.
                tempValue = tempValue.Concat(tempCourtByYear);
            }
            //Create a new array variable to hold the courts 
            var newValue = tempValue.ToArray();

            //Since all the data is pulled, we can then iterate through each record and add the individual stats to the allCourts statistics object. This will give us a single object with each year's statistics for all courts.
            for (int j = 0; j < newValue.Length; j++)
            {
                allCourts.crimFilings += newValue[j].crimFilings;
                allCourts.civilFilings += newValue[j].civilFilings;
                allCourts.trafficFilings += newValue[j].trafficFilings;
                allCourts.crimDispo += newValue[j].crimDispo;
                allCourts.civilDispo += newValue[j].civilDispo;
                allCourts.trafficDispo += newValue[j].trafficDispo;
                //if else statments are used for juvenile and family statistics to verify that NULL values aren't being read. If a null value is read, we simply add 0 to the running total for the allCourts statistics object.
                if (newValue[j].juvFilings is null)
                {
                    allCourts.juvFilings += 0;
                }
                else
                {
                    allCourts.juvFilings += newValue[j].juvFilings;
                }
                if (newValue[j].famFilings is null)
                {
                    allCourts.famFilings += 0;
                }
                else
                {
                    allCourts.famFilings += newValue[j].famFilings;
                }

                if (newValue[j].juvDispo is null)
                {
                    allCourts.juvDispo += 0;
                }
                else
                {
                    allCourts.juvDispo += newValue[j].juvDispo;
                }
                if (newValue[j].famDispo is null)
                {
                    allCourts.famDispo += 0;
                }
                else
                {
                    allCourts.famDispo += newValue[j].famDispo;
                }

            }
            //Include the year for the objects, which would be for a single year, and then append the calculated allCourts object to the empty statisticsData object, which will be returned at the end of the function.
            allCourts.year = year;
            allCourts.courtID = (int)courts["courtIDs"][i];
            statisticData = statisticData.Append(allCourts);
        }
    }

    //Initialize a response array to hold all of the calulated statisticData data.
    var response = statisticData.ToArray();

    //We'll interate through the array to verify that none of the family or juvenile records are NULL.
    for (int i = 0; i < response.Count(); i++)
    {
        if (response[i].famDispo is null)
        {
            response[i].famDispo = 0;
        }
        if (response[i].famFilings is null)
        {
            response[i].famFilings = 0;
        }
        if (response[i].juvDispo is null)
        {
            response[i].juvDispo = 0;
        }
        if (response[i].juvFilings is null)
        {
            response[i].juvFilings = 0;
        }
    }
    return response;
});

app.Run();