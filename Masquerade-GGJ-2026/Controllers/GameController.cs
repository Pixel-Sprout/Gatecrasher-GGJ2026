using Masquerade_GGJ_2026.Models;
using Masquerade_GGJ_2026.Orchestrators;
using Microsoft.AspNetCore.Mvc;

namespace Masquerade_GGJ_2026.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GameController : ControllerBase
    {
        private readonly GameNotifier _notifier;
        private readonly ILogger<GameController> _logger;
        private readonly GameOrchestrator _orchestrator;

        public GameController(ILogger<GameController> logger, GameNotifier notifier, GameOrchestrator orchestrator)
        {
            _notifier = notifier;
            _logger = logger;
            _orchestrator = orchestrator;
        }

        [HttpPost("{gameId}/{playerId}/drawing")]
        public IActionResult UploadDrawing(string gameId, string playerId, [FromBody] string encodedDrawing)
        {
            if (!Guid.TryParse(gameId, out Guid gameGuid))
            {
                return BadRequest(new { error = "Invalid gameId." });
            }

            if (string.IsNullOrWhiteSpace(playerId))
            {
                return BadRequest(new { error = "playerId is required." });
            }

            var game = GamesState.Games.FirstOrDefault(g => g.GameId == gameGuid);
            if (game is null)
            {
                return NotFound(new { error = "Game not found." });
            }

            if (game.PhaseDetails.CurrentPhase != Models.RoundPhase.Drawing)
            {
                return BadRequest(new { error = "Drawings can only be sent during the Drawing phase." });
            }

            var player = game.Players.FirstOrDefault(p => p.ConnectionId == playerId);
            if (player is null)
            {
                return NotFound(new { error = "Player not found in the specified game." });
            }

            if (encodedDrawing is null || string.IsNullOrWhiteSpace(encodedDrawing))
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
        public IActionResult GetPlayerDrawings(string gameId/*, string playerId*/)
        {
            if (!Guid.TryParse(gameId, out Guid gameGuid))
            {
                return BadRequest(new { error = "Invalid gameId." });
            }

            //if (string.IsNullOrWhiteSpace(playerId))
            //{
            //    return BadRequest(new { error = "playerId is required." });
            //}

            var game = GamesState.Games.FirstOrDefault(g => g.GameId == gameGuid);
            if (game is null)
            {
                return NotFound(new { error = "Game not found." });
            }

            if(game.PhaseDetails.CurrentPhase != Models.RoundPhase.Voting)
            {
                return BadRequest(new { error = "Drawings can only be retrieved during the Voting phase." });
            }

            //var player = game.Players.FirstOrDefault(p => p.ConnectionId == playerId);
            //if (player is null)
            //{
            //    return NotFound(new { error = "Player not found in the specified game." });
            //}

            // Return drawings for all players within the game
            var playerDrawings = game.Players
                //.Where(p => p != player && !string.IsNullOrEmpty(p.EncodedMask))
                .ToDictionary(p => p.ConnectionId, p => p.EncodedMask);
            if (playerDrawings.Count > 0)
            {
                return Ok(playerDrawings);
            }
            return NotFound(new { error = "No drawings found." });
        }
    }
}
