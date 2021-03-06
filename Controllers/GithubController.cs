using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using TodoApi.Models;



namespace TodoApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class GithubController : ControllerBase
    {
        private readonly GithubContext _context;
        private readonly String url = "https://api.github.com/users/kloparn/repos";
        static readonly HttpClient client = new HttpClient();


        public GithubController(GithubContext context)
        {
            _context = context;
        }

        // GET: api/Github
        [HttpGet]
        public async Task<ActionResult<IEnumerable<GithubItem>>> GetGithubItems()
        {
            try
            {
                await _context.Database.EnsureDeletedAsync();

                // Setting the user-agent head for github so it does not give 403 forbidden error
                client.DefaultRequestHeaders.Add("User-Agent", "C#App");

                HttpResponseMessage rep = await client.GetAsync(url);

                // Makes sure it gives 200 OK
                rep.EnsureSuccessStatusCode();

                String repBody = await rep.Content.ReadAsStringAsync();

                // Parsing the rep body string to a object.
                object jsonObject = JsonConvert.DeserializeObject<object>(repBody);

                // Making the object loopable by casting it to a IEnumerable
                IEnumerable<object> repos = jsonObject as IEnumerable<object>;

                // Can be null so doing a check
                if (repos != null)
                {
                    foreach (object repo in repos)
                    {
                        // Filtering so it fits to the custom class
                        GithubItem item = GithubItemFilter.filterRepo(repo);

                        // If the filter returns something that should be skipped
                        if (item == null)
                            continue;

                        // Checking if the item is already added or not, so no ID conflict appears.  
                        var githubItem = await _context.GithubItems.FindAsync(item.id);
                        if (githubItem == null)
                            _context.GithubItems.Add(item);
                    }
                }
                // Saving the new github items to the api.
                await _context.SaveChangesAsync();
            }
            catch (HttpRequestException e)
            {
                // Debugging if the http request gets a error.
                Console.WriteLine(e);
            }

            // Converting the githubitems to a list to then be viewed.
            return await _context.GithubItems.ToListAsync();
        }

        // GET: api/Github/5
        [HttpGet("{id}")]
        public async Task<ActionResult<GithubItem>> GetGithubItem(long id)
        {
            var githubItem = await _context.GithubItems.FindAsync(id);

            if (githubItem == null)
            {
                return NotFound();
            }

            return githubItem;
        }

        // PUT: api/Github/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutGithubItem(long id, GithubItem githubItem)
        {
            if (id != githubItem.id)
            {
                return BadRequest();
            }

            _context.Entry(githubItem).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!GithubItemExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Github
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPost]
        public async Task<ActionResult<GithubItem>> PostGithubItem(GithubItem githubItem)
        {
            _context.GithubItems.Add(githubItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetGithubItem), new { id = githubItem.id }, githubItem);
        }

        // DELETE: api/Github/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<GithubItem>> DeleteGithubItem(long id)
        {
            var githubItem = await _context.GithubItems.FindAsync(id);
            if (githubItem == null)
            {
                return NotFound();
            }

            _context.GithubItems.Remove(githubItem);
            await _context.SaveChangesAsync();

            return githubItem;
        }

        private bool GithubItemExists(long id)
        {
            return _context.GithubItems.Any(e => e.id == id);
        }
    }
}
