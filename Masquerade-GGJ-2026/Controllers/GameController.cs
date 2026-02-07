using Masquerade_GGJ_2026.Models;
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
                return NotFound(new { error = "Game not found." });
            }

            if (string.IsNullOrWhiteSpace(playerId))
            {
                return BadRequest(new { error = "playerId is required." });
            }

            if (game.PhaseDetails.CurrentPhase != RoundPhase.Drawing
                && game.PhaseDetails.CurrentPhase != RoundPhase.CutsceneMakeTheMask)
            {
                return BadRequest(new { error = "Drawings can only be sent during the Drawing phase." });
            }

            var player = game.Players.FirstOrDefault(p => p.Player.UserId == playerId);
            if (player is null)
            {
                return NotFound(new { error = "Player not found in the specified game." });
            }

            if (string.IsNullOrWhiteSpace(encodedDrawing))
            {
                return BadRequest(new { error = "encodedDrawing is required in the request body." });
            }

            try
            {
                player.EncodedMask = encodedDrawing;
                _logger.LogInformation("Saved drawing for player {PlayerId}", playerId);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save drawing for player {PlayerId}", playerId);
                return StatusCode(500, new { error = "Failed to save drawing." });
            }
        }

        [HttpGet("{gameId}/drawings")]
        public IActionResult GetPlayerDrawings(string gameId)
        {
            var game = _gameStore.Get(gameId);
            if (game is null)
            {
                return NotFound(new { error = "Game not found." });
            }

            if(game.PhaseDetails.CurrentPhase != RoundPhase.Voting)
            {
                return BadRequest(new { error = "Drawings can only be retrieved during the Voting phase." });
            }

            var playerDrawings = game.Players
                .ToDictionary(p => p.Player.UserId, p => p.EncodedMask);
            if (playerDrawings.Count > 0)
            {
                return Ok(playerDrawings);
            }
            return NotFound(new { error = "No drawings found." });
        }
    }
}
