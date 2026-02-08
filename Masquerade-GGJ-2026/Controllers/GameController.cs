using Masquerade_GGJ_2026.Models.Enums;
using Masquerade_GGJ_2026.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Masquerade_GGJ_2026.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GameController : ControllerBase
    {
        private readonly ILogger<GameController> _logger;
        private readonly IGameStore _gameStore;

        public GameController(ILogger<GameController> logger, IGameStore gameStore)
        {
            _logger = logger;
            _gameStore = gameStore;
        }

        [HttpPost("{gameId}/{playerId}/drawing")]
        public IActionResult UploadDrawing(string gameId, string playerId, [FromBody] string encodedDrawing)
        {
            var game = _gameStore.Get(gameId);
            if (game is null)
            {
                _logger.LogWarning("Failed to upload drawing due to invalid game {GameId}", gameId);
                return NotFound(new { error = "Game not found." });
            }

            if (string.IsNullOrWhiteSpace(playerId))
            {
                _logger.LogWarning("Failed to upload drawing due to empty playerId");
                return BadRequest(new { error = "playerId is required." });
            }

            if (game.PhaseDetails.CurrentPhase != RoundPhase.Drawing
                && game.PhaseDetails.CurrentPhase != RoundPhase.CutsceneMakeTheMask)
            {
                _logger.LogWarning("Failed to upload drawing due to invalid game state ({State}) for {GameId}", game.PhaseDetails.CurrentPhase, gameId);
                return BadRequest(new { error = "Drawings can only be sent during the Drawing phase." });
            }

            var player = game.Players.FirstOrDefault(p => p.Player.UserId == playerId);
            if (player is null)
            {
                _logger.LogWarning("Failed to upload drawing due to player {PlayerId} not being a part of game {GameId}", playerId, gameId);
                return NotFound(new { error = "Player not found in the specified game." });
            }

            if (string.IsNullOrWhiteSpace(encodedDrawing))
            {
                _logger.LogWarning("Failed to upload drawing due to empty drawing string");
                return BadRequest(new { error = "encodedDrawing is required in the request body." });
            }

            player.EncodedMask = encodedDrawing;
            _logger.LogInformation("Saved drawing for player {PlayerId} in game {GameId}", playerId, gameId);
            return Ok();
        }

        [HttpGet("{gameId}/drawings")]
        public IActionResult GetPlayerDrawings(string gameId)
        {
            var game = _gameStore.Get(gameId);
            if (game is null)
            {
                _logger.LogWarning("Failed to load drawings due to invalid game {GameId}", gameId);
                return NotFound(new { error = "Game not found." });
            }

            if (game.PhaseDetails.CurrentPhase != RoundPhase.Voting)
            {
                _logger.LogWarning("Failed to load drawings due to invalid game state ({State}) for {GameId}", game.PhaseDetails.CurrentPhase, gameId);
                return BadRequest(new { error = "Drawings can only be retrieved during the Voting phase." });
            }

            var playerDrawings = game.Players.ToDictionary(p => p.Player.UserId, p => p.EncodedMask);
            _logger.LogInformation("Returned {NumberOfDrawings} for game {GameId}", playerDrawings.Count, gameId);
            if (playerDrawings.Count > 0)
            {
                return Ok(playerDrawings);
            }

            return NotFound(new { error = "No drawings found." });
        }
    }
}
