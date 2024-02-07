using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using MoviesLibraryAPI.Controllers;
using MoviesLibraryAPI.Controllers.Contracts;
using MoviesLibraryAPI.Data.Models;
using MoviesLibraryAPI.Services;
using MoviesLibraryAPI.Services.Contracts;
using System.ComponentModel.DataAnnotations;

namespace MoviesLibraryAPI.Tests
{
    [TestFixture]
    public class NUnitIntegrationTests
    {
        private MoviesLibraryNUnitTestDbContext _dbContext;
        private IMoviesLibraryController _controller;
        private IMoviesRepository _repository;
        IConfiguration _configuration;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
        }

        [SetUp]
        public async Task Setup()
        {
            string dbName = $"MoviesLibraryTestDb_{Guid.NewGuid()}";
            _dbContext = new MoviesLibraryNUnitTestDbContext(_configuration, dbName);

            _repository = new MoviesRepository(_dbContext.Movies);
            _controller = new MoviesLibraryController(_repository);
        }

        [TearDown]
        public async Task TearDown()
        {
            await _dbContext.ClearDatabaseAsync();
        }

        [Test]
        public async Task AddMovieAsync_WhenValidMovieProvided_ShouldAddToDatabase()
        {
            // Arrange
            var movie = new Movie
            {
                Title = "Test Movie",
                Director = "Test Director",
                YearReleased = 2022,
                Genre = "Action",
                Duration = 86,
                Rating = 7.5
            };

            // Act
            await _controller.AddAsync(movie);

            // Assert
            var resultMovie = await _dbContext.Movies.Find(m => m.Title == "Test Movie").FirstOrDefaultAsync();
            Assert.IsNotNull(resultMovie);
        }

        [Test]
        public async Task AddMovieAsync_WhenInvalidMovieProvided_ShouldThrowValidationException()
        {
            // Arrange
            var invalidMovie = new Movie
            {
                // Provide an invalid movie object, for example, missing required fields like 'Title'
                // Assuming 'Title' is a required field, do not set it
                Title = "Random title",
                Director = "Random director",
                YearReleased = 2001,
                Genre = "Action"               
            };

            // Act and Assert
            // Expect a ValidationException because the movie is missing a required field
            var exception = Assert.ThrowsAsync<ValidationException>(() => _controller.AddAsync(invalidMovie));
        }

        [Test]
        public async Task DeleteAsync_WhenValidTitleProvided_ShouldDeleteMovie()
        {
            // Arrange
            var movie = new Movie
            {
                Title = "Test Movie",
                Director = "Test Director",
                YearReleased = 2022,
                Genre = "Action",
                Duration = 86,
                Rating = 7.5
            };

            await _controller.AddAsync(movie);
            // Act
            await _controller.DeleteAsync(movie.Title);
            var result = await _dbContext.Movies.Find(x => x.Title == movie.Title).FirstOrDefaultAsync();

            // Assert
            // The movie should no longer exist in the database
            Assert.IsNull(result);
        }


        [Test]
        public async Task DeleteAsync_WhenTitleIsNull_ShouldThrowArgumentException()
        {
            // Act and Assert
            Assert.ThrowsAsync<ArgumentException>( () => _controller.DeleteAsync(null));
        }

        [Test]
        public async Task DeleteAsync_WhenTitleIsEmpty_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>( () => _controller.DeleteAsync(string.Empty));            
        }

        [Test]
        public async Task DeleteAsync_WhenTitleDoesNotExist_ShouldThrowInvalidOperationException()
        {
            // Act and Assert
            var result = Assert.ThrowsAsync<InvalidOperationException>( () => _controller.DeleteAsync("random title"));
        }

        [Test]
        public async Task GetAllAsync_WhenNoMoviesExist_ShouldReturnEmptyList()
        {
            // Act
            var result = await _controller.GetAllAsync();

            // Assert
            Assert.IsEmpty(result);
        }

        [Test]
        public async Task GetAllAsync_WhenMoviesExist_ShouldReturnAllMovies()
        {
            // Arrange
            var firstMovie = new Movie
            {
                Title = "Taxi",
                Director = "French Guy",
                YearReleased = 2008,
                Genre = "Action",
                Duration = 100,
                Rating = 8
            };

            var secondMovie = new Movie
            {
                Title = "Baguette",
                Director = "Same French Guy",
                YearReleased = 2012,
                Genre = "Action",
                Duration = 116,
                Rating = 6.5
            };
            await _controller.AddAsync(firstMovie);
            await _controller.AddAsync(secondMovie);

            // Act
            var result = await _controller.GetAllAsync();
            var firstMovieInList = result.FirstOrDefault(x => x.Title == firstMovie.Title);
            var secondMovieInList = result.FirstOrDefault( x => x.Title == secondMovie.Title);

            // Assert
            // Ensure that all movies are returned
            Assert.NotNull(result);
            Assert.AreEqual(2, result.Count());
            Assert.AreEqual(firstMovie.Title, firstMovieInList.Title);
            Assert.AreEqual(secondMovie.Title, secondMovieInList.Title);
        }

        [Test]
        public async Task GetByTitle_WhenTitleExists_ShouldReturnMatchingMovie()
        {
            // Arrange
            var firstMovie = new Movie
            {
                Title = "Taxi",
                Director = "French Guy",
                YearReleased = 2008,
                Genre = "Action",
                Duration = 100,
                Rating = 8
            };

            var secondMovie = new Movie
            {
                Title = "Baguette",
                Director = "Same French Guy",
                YearReleased = 2012,
                Genre = "Action",
                Duration = 116,
                Rating = 6.5
            };
            await _controller.AddAsync(firstMovie);
            await _controller.AddAsync(secondMovie);
            // Act

            var result = await _controller.GetByTitle(secondMovie.Title);            

            // Assert
            Assert.NotNull(result);
            Assert.AreEqual(secondMovie.Title, result.Title);
        }

        [Test]
        public async Task GetByTitle_WhenTitleDoesNotExist_ShouldReturnNull()
        {
            // Act       
           var result =  await _controller.GetByTitle("random title");
            // Assert
            Assert.IsNull(result);
        }


        [Test]
        public async Task SearchByTitleFragmentAsync_WhenTitleFragmentExists_ShouldReturnMatchingMovies()
        {
            // Arrange
            var firstMovie = new Movie
            {
                Title = "Taxi",
                Director = "French Guy",
                YearReleased = 2008,
                Genre = "Action",
                Duration = 100,
                Rating = 8
            };

            var secondMovie = new Movie
            {
                Title = "Baguette",
                Director = "Same French Guy",
                YearReleased = 2012,
                Genre = "Action",
                Duration = 116,
                Rating = 6.5
            };
            await _controller.AddAsync(firstMovie);
            await _controller.AddAsync(secondMovie);

            // Act
            var result = await _controller.SearchByTitleFragmentAsync("tax");
            var foundMovie = result.FirstOrDefault();

            // Assert // Should return one matching movie
            Assert.NotNull(result);
            Assert.AreEqual(1, result.Count());
            Assert.AreEqual(firstMovie.Title, foundMovie.Title);
        }

        [Test]
        public async Task SearchByTitleFragmentAsync_WhenNoMatchingTitleFragment_ShouldThrowKeyNotFoundException()
        {
            // Act and Assert
            var result = Assert.ThrowsAsync<KeyNotFoundException>( () => _controller.SearchByTitleFragmentAsync("invalid fragment"));
            Assert.AreEqual("No movies found.", result.Message);
        }

        [Test]
        public async Task UpdateAsync_WhenValidMovieProvided_ShouldUpdateMovie()
        {
            // Arrange

            var movie = new Movie
            {
                Title = "Test Movie",
                Director = "Test Director",
                YearReleased = 2022,
                Genre = "Action",
                Duration = 86,
                Rating = 7.5
            };

            // Act
            await _controller.AddAsync(movie);

            var newRating = 10;
            movie.Rating = newRating;
           
            await _controller.UpdateAsync(movie);

            // Act
            var updatedMovie = _dbContext.Movies.Find(x => x.Title == movie.Title).FirstOrDefault();

            // Assert
            Assert.NotNull(updatedMovie);
            Assert.AreEqual(movie.Title, updatedMovie.Title);
            Assert.AreEqual(movie.Rating, newRating);
        }

        [Test]
        public async Task UpdateAsync_WhenInvalidMovieProvided_ShouldThrowValidationException()
        {
            // Arrange
            // Movie without required fields
            var invalidMovie = new Movie
            {
                // Provide an invalid movie object, for example, missing required fields like 'Title'
                // Assuming 'Title' is a required field, do not set it
                Title = "Random title",
                Director = "Random director",
                YearReleased = 2001,
                Genre = "Action"
            };

            invalidMovie.Title = "updated title";

            // Act and Assert
            Assert.ThrowsAsync<ValidationException>( () => _controller.UpdateAsync(invalidMovie));
        }


        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            await _dbContext.ClearDatabaseAsync();
        }
    }
}
