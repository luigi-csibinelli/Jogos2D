// Gancho de teste: no console do navegador, sanatorio('power'), sanatorio('points 5000')...
// Encaminha para Game.DebugCommand. Não aparece para o jogador.
mergeInto(LibraryManager.library, {
  SanDebug_Expose: function () {
    window.sanatorio = function (cmd) {
      if (typeof SendMessage === 'function') SendMessage('Game', 'DebugCommand', cmd);
      else if (typeof Module !== 'undefined' && Module.SendMessage) Module.SendMessage('Game', 'DebugCommand', cmd);
      return cmd;
    };
  }
});
