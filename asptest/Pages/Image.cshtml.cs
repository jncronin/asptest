using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using System.Data.SqlClient;

namespace asptest.Pages
{
    public class ImageModel : PageModel
    {
        public string Message { get; private set; } = "Hello World";
        public string ImageFile { get; private set; }
        public int Id { get; private set; }
        public string SaveId { get; private set; }
        public string Observer { get; private set; }
        public string MaxImages { get; private set; }

        IHostingEnvironment e;
        string fname;

        const int MAX_IMAGES = 896;

        class LastTouched
        {
            public int LastTouchedId { get; set; }
            public int Operator { get; set; }
            public int Last { get; set; }
        }

        class Results
        {
            public int ResultsId { get; set; }
            public int Observer { get; set; }
            public int Frame { get; set; }
            public string Val { get; set; }
        }

        class MyContext : Microsoft.EntityFrameworkCore.DbContext
        {
            string fname;

            public MyContext(string sqlitefname)
            {
                fname = sqlitefname;
            }

            protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            {
                optionsBuilder.UseSqlite("Data Source=" + fname);
                base.OnConfiguring(optionsBuilder);
            }

            public Microsoft.EntityFrameworkCore.DbSet<LastTouched> LastTouched { get; set; }
            public Microsoft.EntityFrameworkCore.DbSet<Results> Results { get; set; }
        }

        public ImageModel(IHostingEnvironment environment)
        {
            e = environment;

            // build database if it does not exist
            fname = System.IO.Path.Combine(e.ContentRootPath, "db.sqlite");

            using (var ctx = new MyContext(fname))
            {
                ctx.Database.EnsureCreated();
            }
            /*
            if (!(new System.IO.FileInfo(fname)).Exists)
            {
                var ctx = new Microsoft.EntityFrameworkCore.DbContext();

                System.Data.SQLite.SQLiteConnection.CreateFile(fname);

                System.Data.SQLite.SQLiteConnection conn = new System.Data.SQLite.SQLiteConnection("Data Source=" + fname + ";Version=3;");
                conn.Open();

                string ct = "CREATE TABLE lasttouched (observer int, last int)";
                new System.Data.SQLite.SQLiteCommand(ct, conn).ExecuteNonQuery();

                ct = "CREATE TABLE results (observer int, frame int, val VARCHAR(50))";
                new System.Data.SQLite.SQLiteCommand(ct, conn).ExecuteNonQuery();

                conn.Close();
            } */
        }

        int GetObserverNextImage(int observer, MyContext conn)
        {
            int ret;
            var res = conn.LastTouched.FromSql("SELECT * FROM LastTouched WHERE Operator=" + observer.ToString());
            var resl = new List<LastTouched>(res);

            if(resl.Count == 0)
            {
                conn.Database.ExecuteSqlCommand("INSERT INTO LastTouched(Last, Operator) VALUES (0, " + observer.ToString() + ")");
                ret = 1;
            }
            else
            {
                ret = resl[0].Last + 1;
            }

            return ret;
        }

        int GetObserverNextImage(int observer)
        {
            using (var ctx = new MyContext(fname))
            {
                var ret = GetObserverNextImage(observer, ctx);
                return ret;
           }

        }

        void SetObserverImageValue(int observer, string value, int image = -1)
        {
            using (var ctx = new MyContext(fname))
            {
                if (image == -1)
                    image = GetObserverNextImage(observer, ctx);

                ctx.Database.ExecuteSqlCommand("INSERT INTO Results(Frame, Observer, Val) VALUES (" + image.ToString() + ", " + observer.ToString() + "," + value + ")");
                ctx.Database.ExecuteSqlCommand("UPDATE LastTouched SET Last=" +image.ToString() + " WHERE Operator=" + observer.ToString());
            }

            /*
            System.Data.SQLite.SQLiteConnection conn = new System.Data.SQLite.SQLiteConnection("Data Source=" + fname + ";Version=3;");
            conn.Open();

            if(image == -1)
                image = GetObserverNextImage(observer, conn);

            var q = "INSERT INTO results VALUES (" + observer.ToString() + ", " + image.ToString() + ", " + value + ")";
            new System.Data.SQLite.SQLiteCommand(q, conn).ExecuteNonQuery();

            q = "UPDATE lasttouched SET last=" + image.ToString() + " WHERE observer=" + observer.ToString();
            new System.Data.SQLite.SQLiteCommand(q, conn).ExecuteNonQuery();

            conn.Close(); */
        }

        public void OnGet()
        {
            Message = "get called at " + DateTime.Now;

            MaxImages = MAX_IMAGES.ToString();

            var q = Request.Query;

            int observer;
            if(!q.ContainsKey("observer") || !int.TryParse(q["observer"], out observer) || observer < 1 || observer > 20)
            {
                // throw error as no observer selected
                Response.Redirect("noobserver");
                return;
            }

            // save data if any provided
            if(q.ContainsKey("value"))
            {
                int saveid = -1;
                if (q.ContainsKey("saveid"))
                {
                    if (!int.TryParse(q["saveid"], out saveid))
                        saveid = -1;
                }
                SetObserverImageValue(observer, q["value"], saveid);
            }

            int line = GetObserverNextImage(observer);

            if (line == MAX_IMAGES + 1)
            {
                // do something, congrats etc
                Response.Redirect("complete");
                return;
            }

            Id = line - 1;
            Observer = observer.ToString();
            SaveId = line.ToString();

            var ro = System.IO.File.OpenRead(System.IO.Path.Combine(e.WebRootPath, "randomorder.csv"));
            var sr = new System.IO.StreamReader(ro);
            string l = null;

            for (int i = 0; i < line; i++)
                l = sr.ReadLine();

            var ls = l.Split(',')[observer - 1];

            ls = ls.TrimStart('\"').TrimEnd('\"');

            ImageFile = "images/" + ls + ".png";
        }
    }
}