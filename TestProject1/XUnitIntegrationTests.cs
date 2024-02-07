using MongoDB.Driver;
using MoviesLibraryAPI.Controllers;
using MoviesLibraryAPI.Controllers.Contracts;
using MoviesLibraryAPI.Data.Models;
using MoviesLibraryAPI.Services;
using MoviesLibraryAPI.Services.Contracts;
using System.ComponentModel.DataAnnotations;

namespace MoviesLibraryAPI.XUnitTests
{
    public class XUnitIntegrationTests : IClassFixture<DatabaseFixture>
    {
        private readonly MoviesLibraryXUnitTestDbContext _dbContext;
        private readonly IMoviesLibraryController _controller;
        private readonly IMoviesRepository _repository;

        public XUnitIntegrationTests(DatabaseFixture fixture)
        {
            _dbContext = fixture.DbContext;
            _repository = new MoviesRepository(_dbContext.Movies);
            _controller = new MoviesLibraryController(_repository);

            InitializeDatabaseAsync().GetAwaiter().GetResult();
        }

        private async Task InitializeDatabaseAsync()
        {
            await _dbContext.ClearDatabaseAsync();
        }

        [Fact]
        public async Task AddMovieAsync_WhenValidMovieProvided_ShouldAddToDatabase()
        {
            // Arrange
            var movie = new Movie
            {
                Title = "Test Movie",
                Director = "Test Director",
                YearReleased = 2022,
                Genre = "Action",
                Duration = 120,
                Rating = 7.5
            };

            // Act
            await _controller.AddAsync(movie);

            // Assert
            var resultMovie = await _dbContext.Movies.Find(m => m.Title == "Test Movie").FirstOrDefaultAsync();
            Xunit.Assert.NotNull(resultMovie);
            Xunit.Assert.Equal("Test Movie", resultMovie.Title);
            Xunit.Assert.Equal("Test Director", resultMovie.Director);
            Xunit.Assert.Equal(2022, resultMovie.YearReleased);
            Xunit.Assert.Equal("Action", resultMovie.Genre);
            Xunit.Assert.Equal(120, resultMovie.Duration);
            Xunit.Assert.Equal(7.5, resultMovie.Rating);
        }

        [Fact]
        public async Task AddMovieAsync_WhenInvalidMovieProvided_ShouldThrowValidationException()
        {
            // Arrange
            var invalidMovie = new Movie
            {
                Title = "Test Movie",
                Director = "Test Director",
                YearReleased = 2022,
                Genre = "Action"
            };

            // Act and Assert
            var result = Xunit.Assert.ThrowsAsync<ValidationException>( () => _controller.AddAsync(invalidMovie));
        }

        [Fact]
        public async Task DeleteAsync_WhenValidTitleProvided_ShouldDeleteMovie()
        {
            // Arrange            
            var movie = new Movie
            {
                Title = "Test Movie",
                Director = "Test Director",
                YearReleased = 2022,
                Genre = "Action",
                Duration = 120,
                Rating = 7.5
            };

            await _controller.AddAsync(movie);

            // Act
            await _controller.DeleteAsync(movie.Title);
            var result = await _controller.GetAllAsync();
            var resultMovie = result.FirstOrDefault(x => x.Title == movie.Title);

            // Assert
            Xunit.Assert.Empty(result);
            Xunit.Assert.Null(resultMovie);
        }


        [Fact]
        public async Task DeleteAsync_WhenTitleIsNull_ShouldThrowArgumentException()
        {
            // Act and Assert
            var result = Xunit.Assert.ThrowsAsync<ArgumentException>( () => _controller.DeleteAsync(null));           
            Xunit.Assert.Equal("Title cannot be empty.", result.Result.Message);
        }

        [Fact]
        public async Task DeleteAsync_WhenTitleIsEmpty_ShouldThrowArgumentException()
        {
            // Act and Assert
            var result = Xunit.Assert.ThrowsAsync<ArgumentException>( () => _controller.DeleteAsync(string.Empty));
        }

        [Fact]
        public async Task DeleteAsync_WhenTitleDoesNotExist_ShouldThrowInvalidOperationException()
        {
            //InvalidOperationException($"Movie with title '{title}' not found.");
            // Act and Assert
            string invalidTitle = "non existing title";
            var result = Xunit.Assert.ThrowsAsync<InvalidOperationException>( () => _controller.DeleteAsync(invalidTitle));
            Xunit.Assert.Equal($"Movie with title '{invalidTitle}' not found.", result.Result.Message);
        }

        [Fact]
        public async Task GetAllAsync_WhenNoMoviesExist_ShouldReturnEmptyList()
        {
            // Act
            var result = await _controller.GetAllAsync();

            // Assert
            Xunit.Assert.Empty(result);
        }

        [Fact]
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
            var firstMovieFound = result.FirstOrDefault( x => x.Title == firstMovie.Title);
            var secondMovieFound = result.FirstOrDefault(x => x.Title == secondMovie.Title);

            // Assert
            Xunit.Assert.NotNull(result);
            Xunit.Assert.Equal(firstMovie.Title, firstMovieFound.Title);
            Xunit.Assert.Equal(secondMovie.Title, secondMovieFound.Title);

        }

        [Fact]
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

            await _controller.AddAsync(firstMovie);

            // Act
            var result = await _controller.GetByTitle(firstMovie.Title);

            // Assert
            Xunit.Assert.NotNull(result);
            Xunit.Assert.Equal(firstMovie.Title, result.Title);
        }

        [Fact]
        public async Task GetByTitle_WhenTitleDoesNotExist_ShouldReturnNull()
        {
            // Act
            var result = await _controller.GetByTitle("nonexisting title");

            // Assert
            Xunit.Assert.Null(result);
        }


        [Fact]
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
            var result = await _controller.SearchByTitleFragmentAsync(firstMovie.Title);
            var foundMovie = result.FirstOrDefault();

            // Assert 
            Xunit.Assert.NotNull(result);
            Xunit.Assert.Equal(firstMovie.Title, foundMovie.Title);
        }

        [Fact]
        public async Task SearchByTitleFragmentAsync_WhenNoMatchingTitleFragment_ShouldThrowKeyNotFoundException()
        {
            // Act and Assert
            var result = await Xunit.Assert.ThrowsAsync<KeyNotFoundException>( () => _controller.SearchByTitleFragmentAsync("random"));
        }

        [Fact]
        public async Task UpdateAsync_WhenValidMovieProvided_ShouldUpdateMovie()
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

            await _controller.AddAsync(firstMovie);

            firstMovie.Genre = "Sci-fi";

            // Act
            await _controller.UpdateAsync(firstMovie);
            var updatedMovie = await _dbContext.Movies.Find(x => x.Title == firstMovie.Title).FirstOrDefaultAsync();            

            // Assert
            Xunit.Assert.NotNull(updatedMovie);
            Xunit.Assert.Equal(firstMovie.Genre, updatedMovie.Genre);
        }

        [Fact]
        public async Task UpdateAsync_WhenInvalidMovieProvided_ShouldThrowValidationException()
        {
            // Arrange
            var firstMovie = new Movie
            {
                Title = "Taxi",
                Director = "French Guy",
                YearReleased = 2008,
                Genre = "Action"                
            };

            // Act and Assert
            var result = Xunit.Assert.ThrowsAsync<ValidationException>( () => _controller.UpdateAsync(firstMovie));
        }
    }
}
