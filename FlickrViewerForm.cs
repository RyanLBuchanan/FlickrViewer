using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace FlickrViewer
{
    public partial class FlickrViewerForm : Form
    {
        // Use Flickr API key here
        // Flickr link: https://www.flickr.com/services/apps/create/apply/
        private const string KEY = "8482eccd3cae2108474de760d196bd73";

        // Object used to invoke Flickr web service
        private static HttpClient flickrClient = new HttpClient();

        Task<string> flickrTask = null; // Task that queries Flickr

        public FlickrViewerForm()
        {
            InitializeComponent();
        }

        private async void searchButton_Click(object sender, EventArgs e)
        {
            // If flickrTask already running, prompt user
            if (flickrClient?.Status != TaskStatus.RanToCompletion)
            {
                var result = MessageBox.Show("Cancel the current Flickr search?",
                    "Are you sure?", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                // Determine whether user want to cancel prior search
                if (result == DialogResult.No)
                {
                    return;
                }
                else
                {
                    flickrClient.CancelPendingRequests();
                }
            }

            // Flickr's web service URL for searches
            var flickrURL = "https://api.flickr.com/services/rest/?method=" +
                $"flickr.photos.search&api_key={KEY}&" +
                $"tags={inputTextBox.Text.Replace(" ", ",")}" +
                "&tag_mode=all&per_page=500&privacy_filter=1";

            imagesListBox.DataSource = null; // Remove prior data source
            imagesListBox.Items.Clear(); // Clear images
            pictureBox.Image = null; // Clear picture box
            imagesListBox.Items.Add("Loading...");  

            // Invoke Flickr web service to search Flickr with user's tags
            flickrTask = flickrClient.GetStringAsync(flickrURL);

            // Await flickrTask then parse results with XDocument and LINQ
            XDocument flickrXML = XDocument.Parse(await flickrTask);

            // Gather information on all photos
            var flickrPhotos =
                from photo in flickrXML.Descendants("photo")
                let id = photo.Attribute("id").Value
                let title = photo.Attribute("title").Value
                let secret = photo.Attribute("secret").Value
                let server = photo.Attribute("server").Value
                let farm = photo.Attribute("farm").Value
                select new FlickrResult
                {
                    Title = title,
                    URL = $"https://farm{farm}.staticflickr.com/" +
                        $"{server}/{id}_{secret}.jpg"
                };
            imagesListBox.Items.Clear(); 

            // Set ListBox properties only if the results were found 
            if (flickrPhotos.Any())
            {
                imagesListBox.DataSource = flickrPhotos.ToList();
                imagesListBox.DisplayMember = "Title";
            }
            else // No matches were found
            {
                imagesListBox.Items.Add("No matches");
            }
        }

        // Display selected image 
        private async void imagesListBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (imagesListBox.SelectedItems != null)
            {
                string selectedURL = ((FlickrResult) imagesListBox.SelectedItems).URL;

                // Use HttpClient to get selected image's bytes asynchronously
                byte[] imageBytes = await flickrClient.GetByteArrayAsync(selectedURL);

                // Display downloaded image in pictureBox
                MemoryStream memoryStream = new MemoryStream(imageBytes);
                pictureBox.Image = Image.FromStream(memoryStream);
            }
        }
    }
}
